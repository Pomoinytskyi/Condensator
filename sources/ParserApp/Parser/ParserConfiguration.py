class ParserConfiguration:
    MessageBrockerHost = ""
    ExchangeName = ""
    
    DownloadQueueName = ""
    ProcessArticleContentQueueName = ""
    
    ConnectionString = ""
    DatabaseName = ""
    ArticlesCollectionName = ""

    def __init__(self):
        self.MessageBrockerHost = "localhost"
        self.ExchangeName = ""
        self.DownloadQueueName = "Download"
        self.ProcessArticleContentQueueName = "ProcessArticleContent"

        self.ConnectionString = "localhost"
        self.DatabaseName = "Condensator"
        self.ArticlesCollectionName = "Articles"