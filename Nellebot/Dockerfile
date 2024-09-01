FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

#USER $APP_UID

#ARG USER_ID
#ARG GROUP_ID
#
#RUN groupadd --gid ${GROUP_ID} app && \
#    useradd --uid ${USER_ID} --gid app && \
#    mkdir -p /testdir && \
#    chown -R app:app /testdir && \
#    mkdir -p /home/app/.microsoft/usersecrets && \
#    chown -R app:app /home/app

#RUN mkdir -p /keydata && \
#    chown -R $APP_UID:$APP_UID /keydata && \
#    mkdir -p /home/app/.microsoft/usersecrets && mkdir -p /home/app/.certs &&  \
#    chown -R $APP_UID:$APP_UID /home/app

USER $APP_UID