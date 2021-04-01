#! /bin/bash

# empty directory
rm -rf /usr/nellebot

# recreate it
mkdir -p /usr/nellebot

# copy files from deploy to app directory
cp -r /usr/nellebot-deploy/github/workspace/deploy/. /usr/nellebot