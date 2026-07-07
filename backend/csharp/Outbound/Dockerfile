FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ClassLibraryCore/.", "ClassLibraryCore/"]
COPY ["MicroServicesCore/contact-center.web/backend/csharp/Outbound/.", "MicroServicesCore/contact-center.web/backend/csharp/Outbound/"]

RUN dotnet restore "MicroServicesCore/contact-center.web/backend/csharp/Outbound/Outbound.csproj"

COPY . .
RUN dotnet publish "MicroServicesCore/contact-center.web/backend/csharp/Outbound/Outbound.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl netcat-openbsd ca-certificates && \
    rm -rf /var/lib/apt/lists/*
USER $APP_UID

ENTRYPOINT ["dotnet", "Outbound.dll"]
