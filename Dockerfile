FROM ubuntu:latest
RUN apt-get update -y && \
    apt-get install -y --no-install-recommends wget curl git xz-utils python2.7 ca-certificates && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends tzdata && \
    wget https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb \
        --no-check-certificate \
        -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get install -y --no-install-recommends apt-transport-https && \
    apt-get update -y && \
    apt-get install -y --no-install-recommends dotnet-sdk-5.0 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /usr/src/RelaxHackathon.Compression
COPY . /usr/src/RelaxHackathon.Compression

RUN dotnet restore && \
    dotnet build -c Release && \
    chmod +x RelaxHackathon.Compression/bin/Release/net5.0/RelaxHackathon.Compression && \
    git clone https://github.com/precious/bash_minifier.git bash_minifier

WORKDIR /app

ENTRYPOINT [ "/usr/src/vehicle-routing/run.sh" ]
