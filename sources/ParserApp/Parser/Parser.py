import datetime
import json
from collections import namedtuple
import pika
import pymongo
from . import ParserConfiguration
from nltk.tokenize import sent_tokenize
from bson.objectid import ObjectId

import asyncio
import logging
import nltk
from telegram import Update
from telegram.ext import ApplicationBuilder, ContextTypes, CommandHandler, filters, MessageHandler

from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
from transformers import pipeline

from goose3 import Goose
import torch

from nltk.tokenize import sent_tokenize, word_tokenize
from time import perf_counter
import logging;

class Parser:
    ParagraphMaxLength = 1000;
    logger = logging.getLogger()
    def __init__(self, configuration: ParserConfiguration):
        self.configuration = configuration
        client = pymongo.MongoClient(self.configuration.ConnectionString)
        database = client[self.configuration.DatabaseName]
        self.ArticlesCollection = database[self.configuration.ArticlesCollectionName]
        self.summarizer = pipeline("summarization", model="facebook/bart-large-cnn", device=torch.cuda.current_device())

    def customConfigurationDecoder(self, configDict) -> ParserConfiguration:
        return namedtuple('X', configDict.keys())(*configDict.values())

    def OnNewParseRequest(self, channel, method, properties, body):
        articleId = body.decode('utf-8')
        self.logger.debug("New message received: {articleId}", articleId = articleId)
        cleanedText = self.LoadArticleFromDb(articleId)
        if len(cleanedText) > 0:
            self.logger.debug("Article loaded {articleId}", articleId = articleId)
            paragraphs = self.SpleateOnTokens(cleanedText)
            if len(paragraphs) > 1:
                summary = self.Summarise(paragraphs)
                self.SaveSummaryToDb(articleId, summary)
                self.logger.debug("Summary saved to DB")
            else:
                self.SaveSummaryToDb(articleId, "!!! No text content detected !!!")
                self.logger.warning("No text contend found in article {articleId}, {cleanedText}", articleId = articleId, cleanedText = cleanedText)
        channel.basic_ack(delivery_tag = method.delivery_tag)

    def StartListening(self):
        connection = pika.BlockingConnection(
            pika.ConnectionParameters(
                host=self.configuration.MessageBrockerHost))

        channel = connection.channel()
        channel.queue_declare(queue=self.configuration.ProcessArticleContentQueueName, durable = True)

        channel.basic_consume(
            queue = self.configuration.ProcessArticleContentQueueName,
            on_message_callback = self.OnNewParseRequest)

        print('Waiting for messages. To exit press CTRL+C')
        channel.start_consuming()

    def LoadArticleFromDb(self, articleId:str) -> list:
        article = self.ArticlesCollection.find_one(ObjectId(articleId))
        if article == None : 
            return ""
        return article["CleanedText"]

    def SaveSummaryToDb(self, articleId: str, summary: str) -> str:
        updateResult = self.ArticlesCollection.update_one({"_id": ObjectId(articleId)}, {"$set": {"Summary": summary}})
        return updateResult

    def SpleateOnTokens(self, mainText: str) -> list:
        tokens = filter(
                    lambda s: len(s)>0,
                    mainText.split("\n"))

        paragraphs = []
        wasOpenSentence = False

        for token in tokens:
            sentences = sent_tokenize(token)
            sentencesNumber = len(sentences)
            
            sentence = sentences[sentencesNumber - 1]
            lastChar = sentence[len(sentence) - 1]

            if wasOpenSentence or sentencesNumber == 1:
                insertIndex = len(paragraphs) - 1
                if insertIndex >= 0:
                    paragraphs[insertIndex] =  paragraphs[insertIndex] +"\n"+ token
                else: paragraphs.append(token) 
            else:
                paragraphs.append(token)
            wasOpenSentence = lastChar == ":" or lastChar == ";" or lastChar == ","
        
        joined = paragraphs[0]
        result = []
        for paragraph in paragraphs[1:] :
            if len(joined) + len(paragraph) + 2 < self.ParagraphMaxLength:
                joined = joined + " \n" + paragraph
            else:
                result.append(joined)
                joined = paragraph
        return result
    
    def Summarise(self, tokens: list) -> str :
        summary = self.GetSummary(tokens[0])
        for token in tokens[1:]:
            summary = summary + self.GetSummary(token)
        return summary

    def GetSummary(self, text: str) -> str:
        summaryObject = self.summarizer(text, max_length=100, min_length=50, do_sample=False)
        summary = summaryObject[0]['summary_text']
        return summary
