#!/usr/bin/env python
import json
from collections import namedtuple



import sys
import os

from Downloader import Downloader, DownloaderConfiguration

configurationPath = "./sources/DownloaderApp/configuration.json"

def customConfigurationDecoder(configDict) -> DownloaderConfiguration:
    return namedtuple('X', configDict.keys())(*configDict.values())

def ReadJsonConfigurationFromFile(filename) -> DownloaderConfiguration:
    with open(filename) as openFile:
        return json.load(openFile, object_hook=customConfigurationDecoder)

def SaveConfigurationToJsonFile(configuration: DownloaderConfiguration, filename: str):
    with open(filename, 'w') as openFile:
        json.dump(configuration.__dict__, openFile, indent=4)

if __name__ == '__main__':
    configuration : DownloaderConfiguration = ReadJsonConfigurationFromFile(configurationPath)
    # configuration = DownloaderConfiguration()
    # SaveConfigurationToJsonFile(configuration, configurationPath)

    downloader = Downloader(configuration)

    try:
        downloader.StartListening()
    except KeyboardInterrupt:
        print('Interrupted')
        try:
            sys.exit(0)
        except SystemExit:
            os._exit(0)