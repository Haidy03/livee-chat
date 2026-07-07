# Stage 1: Build Frontend (React/Vite)
FROM node:22-alpine AS web-build
WORKDIR /web

COPY MicroServicesCore/contact-center.web/package*.json ./
RUN npm install
COPY MicroServicesCore/contact-center.web/ ./
RUN NODE_OPTIONS="--max-old-space-size=4096" npm run build
# Report export chart bundle (Recharts) embedded by the backend HTML/PDF renderers.
RUN npm run build:report-bundle

# Stage 2: Build Backend (.NET API)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src

COPY ["ClassLibraryCore/.", "ClassLibraryCore/"]
COPY ["AI-Tools/.", "AI-Tools/"]
COPY ["MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/.", "MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/"]

RUN dotnet restore MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/src/VoiceFlow.Api/VoiceFlow.Api.csproj

COPY . .
RUN dotnet publish MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/src/VoiceFlow.Api/VoiceFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Chromium for headless report PDF rendering. Installed in the image (not downloaded at
# runtime) and pinned via PUPPETEER_EXECUTABLE_PATH so pods never re-fetch it.
RUN apt-get update && \
    apt-get install -y --no-install-recommends chromium fonts-liberation && \
    rm -rf /var/lib/apt/lists/*
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

COPY --from=api-build /app/publish .

COPY --from=api-build /src/MicroServicesCore/contact-center.web/backend/csharp/Contact-Center.Api/src/VoiceFlow.Api/keys ./keys

COPY --from=web-build /web/dist ./wwwroot

# Freshly-built chart bundle from the web stage (overrides any stale committed copy).
COPY --from=web-build /web/backend/csharp/Contact-Center.Api/src/VoiceFlow.Infrastructure/Reports/Rendering/assets/report-charts.js ./Reports/Rendering/assets/report-charts.js

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VoiceFlow.Api.dll"]
