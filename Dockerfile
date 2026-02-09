# ══════════════════════════════════════════════════════════════════════
# MERSEL.Services.HtmlToPdf — Multi-stage Docker Build
# ══════════════════════════════════════════════════════════════════════

# ── Build Stage ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY global.json .
COPY MERSEL.Services.HtmlToPdf.sln .
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Web/Web.csproj src/Web/

RUN dotnet restore src/Web/Web.csproj

COPY src/ src/
RUN dotnet publish src/Web/Web.csproj -c Release -o /app/publish

# ── Runtime Stage ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Chromium sistem bağımlılıkları + fontlar
RUN for i in 1 2 3; do \
        apt-get update && apt-get install -y --no-install-recommends --fix-missing \
            libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
            libdrm2 libdbus-1-3 libxkbcommon0 libatspi2.0-0 libxcomposite1 \
            libxdamage1 libxfixes3 libxrandr2 libgbm1 libpango-1.0-0 \
            libcairo2 libasound2 libwayland-client0 \
            curl fonts-liberation fonts-noto-color-emoji fonts-noto-cjk \
        && break || (echo "Retry $i..." && sleep 5); \
    done \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Chromium'u Playwright'ın kendi installer'ı ile ön-yükle
# Microsoft.Playwright.dll doğrudan çalıştırılabilir (.NET runtime yeterli)
RUN dotnet exec --runtimeconfig Web.runtimeconfig.json Microsoft.Playwright.dll install chromium

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -sf http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Web.dll"]
