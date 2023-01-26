'''
Downloader Module documentation
'''
class DownloaderConfiguration:
    '''
    Handle application configuration
    '''

    MessageBrockerHost = ""
    ExchangeName = ""
    
    DownloadQueueName = ""
    ProcessArticleContentQueueName = ""
    
    ConnectionString = ""
    DatabaseName = ""
    ArticlesCollectionName = ""

    def __init__(self):
        '''
        Populate with default values
        '''
        self.MessageBrockerHost = "localhost"
        self.ExchangeName = ""
        self.DownloadQueueName = "Download"
        self.ProcessArticleContentQueueName = "ProcessArticleContent"

        self.ConnectionString = "localhost"
        self.DatabaseName = "Condensator"
        self.ArticlesCollectionName = "Articles"