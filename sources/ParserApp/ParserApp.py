#!/usr/bin/env python
import json
from collections import namedtuple

import sys
import os

from Parser import Parser, ParserConfiguration

configurationPath = "./sources/configuration.json"

def customConfigurationDecoder(configDict) -> ParserConfiguration:
    return namedtuple('X', configDict.keys())(*configDict.values())

def ReadJsonConfigurationFromFile(filename) -> ParserConfiguration:
    with open(filename) as openFile:
        return json.load(openFile, object_hook=customConfigurationDecoder)

def SaveConfigurationToJsonFile(configuration: ParserConfiguration, filename: str):
    with open(filename, 'w') as openFile:
        json.dump(configuration.__dict__, openFile, indent=4)

if __name__ == '__main__':
    configuration : ParserConfiguration = ReadJsonConfigurationFromFile(configurationPath)
    parser = Parser(configuration)

    try:
        parser.StartListening()
    except KeyboardInterrupt:
        print('Interrupted')
        try:
            sys.exit(0)
        except SystemExit:
            os._exit(0)