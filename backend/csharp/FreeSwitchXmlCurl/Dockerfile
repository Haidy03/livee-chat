FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MicroServicesCore/contact-center.web/backend/csharp/FreeSwitchXmlCurl/FreeSwitch.csproj", "MicroServicesCore/contact-center.web/backend/csharp/FreeSwitchXmlCurl/"]
RUN dotnet restore "MicroServicesCore/contact-center.web/backend/csharp/FreeSwitchXmlCurl/FreeSwitch.csproj"
COPY ["MicroServicesCore/contact-center.web/backend/csharp/FreeSwitchXmlCurl/.", "MicroServicesCore/contact-center.web/backend/csharp/FreeSwitchXmlCurl/"]

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MicroServicesCore/contact-center.web/backend/csharp/FreeSwitchXmlCurl/FreeSwitch.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl netcat-openbsd ca-certificates && \
    rm -rf /var/lib/apt/lists/*
USER $APP_UID

ENTRYPOINT ["dotnet", "FreeSwitch.dll"]
