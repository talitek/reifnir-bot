#! /bin/bash

# empty directory
rm -rf /home/nellebot-user/nellebot

# recreate it
mkdir -p /home/nellebot-user/nellebot

# copy files from deploy to app directory
cp -r /home/nellebot-user/nellebot-deploy/github/workspace/deploy/. /home/nellebot-user/nellebot