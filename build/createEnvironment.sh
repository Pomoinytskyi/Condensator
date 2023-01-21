#!/bin/bash


# Help
Help()
{
   # Display Help
   echo "Run docker compose to prepare all reqired services."
   echo
   echo "Syntax: scriptTemplate [-h|p]"
   echo "options:"
   echo "h     Print this Help."
   echo "p     Set Docker Compose project name."
   echo
}

# Parse Parameters Loop: #

while getopts ":hp:" option; do
   case $option in
      h) # display Help
         Help
         exit;;
      n) # Enter a name
         COMPOSE_PROJECT_NAME=$OPTARG;;
     \?) # Invalid option
         echo "Error: Invalid option"
         exit;;
   esac
done

# Main Part: #
docker compose up -d