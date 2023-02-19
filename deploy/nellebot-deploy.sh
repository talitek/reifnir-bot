#! /bin/bash

# remove directory
rm -rf $HOME/nellebot
mkdir -p $HOME/nellebot

# copy files from staging to app directory
cp -r $HOME/nellebot-staging/. $HOME/nellebot

# copy service file and create path if it doesn't exist
mkdir -p $HOME/.config/systemd/user
cp $HOME/nellebot-deploy/nellebot.service $HOME/.config/systemd/user/