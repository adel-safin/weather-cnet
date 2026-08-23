# syntax=docker/dockerfile:1

# Сборка идёт в отдельном слое: в финальный образ не попадают ни SDK, ни исходники
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /source

# Сначала только файлы описания зависимостей: слой restore переиспользуется, пока не изменились ссылки на пакеты
# .editorconfig нужен и в сборке: в нём настроены анализаторы, а предупреждения в этом решении трактуются как ошибки
COPY global.json Directory.Build.props Directory.Packages.props Weather.slnx .editorconfig ./
COPY src/Weather.Domain/Weather.Domain.csproj src/Weather.Domain/
COPY src/Weather.Application/Weather.Application.csproj src/Weather.Application/
COPY src/Weather.Infrastructure/Weather.Infrastructure.csproj src/Weather.Infrastructure/
COPY src/Weather.Web/Weather.Web.csproj src/Weather.Web/
RUN dotnet restore src/Weather.Web/Weather.Web.csproj

COPY src/ src/
RUN dotnet publish src/Weather.Web/Weather.Web.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl нужен только для HEALTHCHECK; кэш пакетов в образ не тянем
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1

EXPOSE 8080

# APP_UID объявлен в базовом образе: приложение работает без прав root
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Weather.Web.dll"]
