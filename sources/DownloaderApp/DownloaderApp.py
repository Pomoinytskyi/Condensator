#!/usr/bin/env python
import json
from collections import namedtuple
import sys
import os
from Downloader import Downloader, DownloaderConfiguration

import seqlog
import logging

seqlog.log_to_seq(
   server_url="http://localhost:5341/",
   api_key="",
   level=logging.DEBUG,
   batch_size=10,
   auto_flush_timeout=1,  # seconds
   override_root_logger=True,
   json_encoder_class=json.encoder.JSONEncoder  # Optional; only specify this if you want to use a custom JSON encoder
)

logger = logging.getLogger()

configurationPath = "./sources/configuration.json"

def customConfigurationDecoder(configDict) -> DownloaderConfiguration:
    return namedtuple('X', configDict.keys())(*configDict.values())

def ReadJsonConfigurationFromFile(filename) -> DownloaderConfiguration:
    with open(filename) as openFile:
        return json.load(openFile, object_hook=customConfigurationDecoder)

def SaveConfigurationToJsonFile(configuration: DownloaderConfiguration, filename: str):
    with open(filename, 'w') as openFile:
        json.dump(configuration.__dict__, openFile, indent=4)

if __name__ == '__main__':
    logger.info("Downloader started")
    configuration : DownloaderConfiguration = ReadJsonConfigurationFromFile(configurationPath)
    downloader = Downloader(configuration)

    try:
        downloader.StartListening()
    except KeyboardInterrupt:
        print('Interrupted')
        try:
            sys.exit(0)
        except SystemExit:
            os._exit(0)