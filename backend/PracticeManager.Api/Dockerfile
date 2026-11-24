# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/PracticeManager.Api/PracticeManager.Api.csproj backend/PracticeManager.Api/
RUN dotnet restore backend/PracticeManager.Api/PracticeManager.Api.csproj

COPY backend/PracticeManager.Api/ backend/PracticeManager.Api/
WORKDIR /src/backend/PracticeManager.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY templates /app/templates
RUN mkdir -p /app/Storage/documents

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=1 \
    Storage__Root=/app/Storage/documents \
    Templates__Root=/app/templates

EXPOSE 8080
ENTRYPOINT ["dotnet", "PracticeManager.Api.dll"]
