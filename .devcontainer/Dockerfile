FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        git \
        jq \
        lib32gcc-s1 \
        lib32stdc++6 \
        python3 \
        python3-pip \
        unzip \
        zip \
    && rm -rf /var/lib/apt/lists/*

RUN python3 -m pip install --break-system-packages --no-cache-dir \
    google-api-python-client \
    google-auth \
    google-auth-httplib2

RUN curl -fsSL https://github.com/cli/cli/releases/download/v2.79.0/gh_2.79.0_linux_amd64.tar.gz | tar -xz -C /tmp \
    && install /tmp/gh_2.79.0_linux_amd64/bin/gh /usr/local/bin/gh \
    && rm -rf /tmp/gh_2.79.0_linux_amd64
