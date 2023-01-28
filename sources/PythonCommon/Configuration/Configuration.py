'''
Downloader Module documentation
'''
class Configuration:
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

def customConfigurationDecoder(configDict) -> DownloaderConfiguration:
    return namedtuple('X', configDict.keys())(*configDict.values())

def ReadJsonConfigurationFromFile(filename) -> DownloaderConfiguration:
    with open(filename) as openFile:
        return json.load(openFile, object_hook=customConfigurationDecoder)

def SaveConfigurationToJsonFile(configuration: DownloaderConfiguration, filename: str):
    with open(filename, 'w') as openFile:
        json.dump(configuration.__dict__, openFile, indent=4)