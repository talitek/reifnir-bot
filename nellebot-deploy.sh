#! /bin/bash

# create folder if it doesnt'exit
mkdir -p /usr/nellebot

# empty directory
rm -r /usr/nellebot

# copy files from deploy to app directory
cp -r /usr/nellebot-deploy/github/workspace/deploy/. /usr/nellebot