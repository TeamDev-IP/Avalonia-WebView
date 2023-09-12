FROM mcr.microsoft.com/devcontainers/dotnet:0-6.0 AS dev 

# XVFB and other dependencies
RUN apt-get update && \
    apt-get -y install --no-install-recommends \
        libgtk2.0-0 \
        libgtk-3-0 \
        libgbm1 \
        libnotify4 \
        libgconf-2-4 \
        libnss3 \
        libxss1 \
        libasound2 \
        libxtst6 \
        xauth \
        xvfb \
        x11-xserver-utils

ENV DISPLAY :0

ADD xvfb_init.sh /etc/init.d/xvfb
RUN chmod a+x /etc/init.d/xvfb
