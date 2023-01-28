import datetime
import json
from collections import namedtuple
import pika

import pymongo

from . import DownloaderConfiguration

from goose3 import Goose
from goose3.article import Article as GooseArticle
from nltk.tokenize import sent_tokenize

class DownloadRequestMessage:
    Url = ""
    SourceId = ""
    TimeStamp = datetime.datetime.now()

    def __init__(self, url: str, sourceId: str, timeStamp: datetime):
        self.Url = url
        self.SourceId = sourceId
        self.TimeStamp = timeStamp

class NewsPleaseArticle:
    def __init__(self, authors, downloadDate, modifyDate, publishDate, 
                 description, imageUrl, language, title, titlePage, 
                 titleRss, sourceDomain, mainText, url):
        self.Authors = authors
        self.DownloadDate = downloadDate
        self.ModifyDate = modifyDate
        self.PublishDate = publishDate
        self.Description = description
        self.ImageUrl = imageUrl
        self.Language = language
        self.Title = title
        self.TitlePage = titlePage
        self.TitleRss = titleRss
        self.SourceDomain = sourceDomain
        self.MainText = mainText
        self.Url = url

    def __init__(self, newsParserArticle):
        self.Authors = newsParserArticle.authors
        self.DownloadDate = newsParserArticle.downloadDate
        self.ModifyDate = newsParserArticle.modifyDate
        self.PublishDate = newsParserArticle.publishDate
        self.Description = newsParserArticle.description
        self.ImageUrl = newsParserArticle.imageUrl
        self.Language = newsParserArticle.language
        self.Title = newsParserArticle.title
        self.TitlePage = newsParserArticle.titlePage
        self.TitleRss = newsParserArticle.titleRss
        self.SourceDomain = newsParserArticle.sourceDomain
        self.MainText = newsParserArticle.mainText
        self.Url = newsParserArticle.url

class ArticleInfo:

    Title: str = ""
    CleanedText: str = ""
    MetaDescription: str = ""
    MetaLang: str = ""
    MetaFavicon : str = ""
    MetaKeywords : str = ""
    MetaEncoding : str = ""
    CanonicalLink : str = ""
    Domain: str = ""
    TopNode : str = ""
    TopImage : str = ""
    Tags : list = []
    Opengraph = []
    Tweets: list = []
    Movies: list = []
    Links : list = []
    Authors : list = []
    FinalUrl : str = ""
    LinkHash : str = ""
    RawHtml : str = ""
    Schema : str = ""
    Doc : str = ""
    RawDoc : str = ""
    PublishDate : str = ""
    PublishDatetimeUtc: str = ""
    AdditionalData: str = ""
    Paragraphs: list = []

    def __init__(self, gooseArticle:GooseArticle):
        self.Title = gooseArticle.title
        self.CleanedText = gooseArticle.cleaned_text
        self.MetaDescription = gooseArticle.meta_description
        self.MetaLang = gooseArticle.meta_lang
        self.MetaFavicon = gooseArticle.meta_favicon
        self.MetaKeywords = gooseArticle.meta_keywords
        self.MetaEncoding = gooseArticle.meta_encoding
        self.CanonicalLink = gooseArticle.canonical_link
        self.Domain = gooseArticle.domain
        # self.TopNode = gooseArticle.top_node
        self.TopImage = gooseArticle.top_image
        self.Tags = gooseArticle.tags
        self.Opengraph = gooseArticle.opengraph
        self.Tweets = gooseArticle.tweets
        self.Movies = gooseArticle.movies
        self.Links = gooseArticle.links
        self.Authors = gooseArticle.authors
        self.FinalUrl = gooseArticle.final_url
        self.LinkHash = gooseArticle.link_hash
        # self.RawHtml = gooseArticle.raw_html
        # self.Schema = gooseArticle.schema
        # self.Doc = gooseArticle.doc
        # self.RawDoc = gooseArticle.raw_doc
        self.PublishDate = gooseArticle.publish_date
        self.PublishDatetimeUtc = gooseArticle.publish_datetime_utc
        self.AdditionalData = gooseArticle.additional_data
        
        # self.Paragraphs = self.SpleateOnParagraphs(gooseArticle.cleaned_text)


    def SpleateOnParagraphs(self, mainText: str) -> list:
        tokens = filter(
                    lambda s: len(s)>0,
                    mainText.split("\n"))

        result = []
        wasOpenSentence = False

        for token in tokens:
            sentences = sent_tokenize(token)
            sentencesNumber = len(sentences)
            
            sentence = sentences[sentencesNumber - 1]
            lastChar = sentence[len(sentence) - 1]

            if wasOpenSentence or sentencesNumber == 1:
                insertIndex = len(result) - 1
                result[insertIndex] =  result[insertIndex] +"\n"+ token
            else:
                result.append(token)
            wasOpenSentence = lastChar == ":" or lastChar == ";" or lastChar == ","
        
        return result
   
class Downloader:
    def __init__(self, configuration: DownloaderConfiguration):
        self.configuration = configuration
        self.Goose = Goose()

    def customConfigurationDecoder(self, configDict) -> DownloaderConfiguration:
        return namedtuple('X', configDict.keys())(*configDict.values())

    def OnNewDownloadRequest(self, channel, method, properties, body):
        requestObject = json.loads(body, object_hook=self.customConfigurationDecoder )
        gooseArticle = self.Goose.extract(url=requestObject.Url)
        articleId = self.SaveArticleToDb(ArticleInfo(gooseArticle))
        self.AckMessageProcessed(channel, method, articleId)
       
    def AckMessageProcessed(self, channel, method, articleId: str):
        channel.basic_ack(delivery_tag = method.delivery_tag) # Ack that message was processed. Default timeout 30 min
        channel.basic_publish(
            exchange=self.configuration.ExchangeName,
            routing_key = self.configuration.ProcessArticleContentQueueName,
            body=articleId)

    def StartListening(self):
        connection = pika.BlockingConnection(
            pika.ConnectionParameters(
                host=self.configuration.MessageBrockerHost))

        channel = connection.channel()
        channel.queue_declare(queue=self.configuration.DownloadQueueName, durable = True)
        channel.queue_declare(queue=self.configuration.ProcessArticleContentQueueName, durable = True)

        channel.basic_consume(
            queue = self.configuration.DownloadQueueName,
            on_message_callback = self.OnNewDownloadRequest)

        print('Waiting for messages. To exit press CTRL+C')
        channel.start_consuming()

    def SaveArticleToDb(self, article : ArticleInfo) -> str:
        client = pymongo.MongoClient(self.configuration.ConnectionString)
        database = client[self.configuration.DatabaseName]
        articlesCollection = database[self.configuration.ArticlesCollectionName]
        insertResult = articlesCollection.insert_one(article.__dict__)
        return insertResult.inserted_id.__str__()
    