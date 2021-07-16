#! /bin/bash

# create db backup in tmp folder
su - postgres -c pg_dump nellebot > /tmp/nellebot.bak

# create folder if it doesnt'exit
mkdir -p /home/nellebot-user/nellebot-db-backups

# move backup file to backups folder
mv /tmp/nellebot.bak /home/nellebot-user/nellebot-db-backups/nellebot-$(date +%Y-%m-%d-%H:%M).bak