FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ClassLibraryCore/.", "ClassLibraryCore/"]
COPY ["MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/.", "MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/"]
COPY ["MicroServicesCore/contact-center.web/backend/csharp/ContactCenterService/.", "MicroServicesCore/contact-center.web/backend/csharp/ContactCenterService/"]

RUN dotnet restore "MicroServicesCore/contact-center.web/backend/csharp/ContactCenterService/ContactCenterService/ContactCenterService.csproj"

COPY . .
RUN dotnet publish "MicroServicesCore/contact-center.web/backend/csharp/ContactCenterService/ContactCenterService/ContactCenterService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER root
# Chromium for headless report PDF rendering (scheduled exports). Installed in the image,
# pinned via PUPPETEER_EXECUTABLE_PATH so the worker never downloads it at runtime.
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl netcat-openbsd ca-certificates chromium fonts-liberation && \
    rm -rf /var/lib/apt/lists/*
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
USER $APP_UID

ENTRYPOINT ["dotnet", "ContactCenterService.dll"]
