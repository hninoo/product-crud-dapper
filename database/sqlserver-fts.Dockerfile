FROM mcr.microsoft.com/mssql/server:2022-latest

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
    && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
        | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" \
        > /etc/apt/sources.list.d/mssql-server.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends mssql-server-fts \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
USER mssql
