FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /build

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY deploy/docker/scripts/install-build-ca.sh /tmp/install-build-ca.sh
COPY certs/ /tmp/build-certs/
RUN sed -i 's/\r$//' /tmp/install-build-ca.sh \
    && chmod +x /tmp/install-build-ca.sh \
    && /tmp/install-build-ca.sh /tmp/build-certs \
    && rm -rf /tmp/build-certs /tmp/install-build-ca.sh

COPY src/DirectorySync/*.csproj src/DirectorySync/
COPY src/DirectorySync.Application/*.csproj src/DirectorySync.Application/
COPY src/DirectorySync.Shared/*.csproj src/DirectorySync.Shared/
COPY src/DirectorySync.Infrastructure/*.csproj src/DirectorySync.Infrastructure/
COPY src/DirectorySync.Infrastructure.Shared/*.csproj src/DirectorySync.Infrastructure.Shared/
COPY src/hosts/DirectorySync.Host.Console/*.csproj src/hosts/DirectorySync.Host.Console/

COPY Directory.Packages.props .
COPY nuget.config .

RUN dotnet restore \
    src/hosts/DirectorySync.Host.Console/DirectorySync.Host.Console.csproj \
    --configfile ./nuget.config

COPY src/ src/

WORKDIR /build/src/hosts/DirectorySync.Host.Console

RUN dotnet publish \
    DirectorySync.Host.Console.csproj \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore \
    --self-contained false \
    /p:UseAppHost=false \
    /p:PublishSingleFile=false \
    /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        bash \
        ca-certificates \
        openssl \
        procps \
        libldap-2.5-0 \
        libldap-common \
        ldap-utils  \
        libsasl2-2 \
        libsasl2-modules \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /etc/ldap \
    && printf 'TLS_REQCERT demand\nTLS_CACERTDIR /certs\n' > /etc/ldap/ldap.conf

RUN addgroup --system directorysync \
    && adduser --system \
        --ingroup directorysync \
        --home /app \
        --shell /usr/sbin/nologin \
        directorysync

WORKDIR /app

COPY --from=build /app/publish ./
COPY deploy/docker/scripts/entrypoint.sh /entrypoint.sh
COPY deploy/docker/scripts/healthcheck.sh /app/healthcheck.sh

RUN sed -i 's/\r$//' /entrypoint.sh /app/healthcheck.sh \
    && chmod 0755 /entrypoint.sh /app/healthcheck.sh \
    && mkdir -p /app/data /app/logs /certs \
    && chown -R directorysync:directorysync /app /certs

ENV DOTNET_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DIRECTORYSYNC_RUNTIME_MODE=Docker \
    DIRECTORYSYNC_STORAGE__DIRECTORY=/app/data \
    DIRECTORYSYNC_LOGGING__FILE__PATH=/app/logs/log-.txt \
    LDAPTLS_CACERTDIR=/certs

USER directorysync

ENTRYPOINT ["/entrypoint.sh"]
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD ["/app/healthcheck.sh"]

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS dev

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY deploy/docker/scripts/install-build-ca.sh /tmp/install-build-ca.sh
COPY certs/ /tmp/build-certs/
RUN sed -i 's/\r$//' /tmp/install-build-ca.sh \
    && chmod +x /tmp/install-build-ca.sh \
    && /tmp/install-build-ca.sh /tmp/build-certs \
    && rm -rf /tmp/build-certs /tmp/install-build-ca.sh

WORKDIR /workspace

COPY nuget.config .

ENV DOTNET_ENVIRONMENT=Development \
    DOTNET_USE_POLLING_FILE_WATCHER=1 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DIRECTORYSYNC_RUNTIME_MODE=Docker \
    LDAPTLS_CACERTDIR=/certs

EXPOSE 8080

CMD ["bash"]
