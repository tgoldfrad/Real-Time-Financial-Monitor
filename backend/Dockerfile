# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY FinancialMonitor.Api/FinancialMonitor.Api.csproj FinancialMonitor.Api/
RUN dotnet restore FinancialMonitor.Api/FinancialMonitor.Api.csproj

COPY FinancialMonitor.Api/ FinancialMonitor.Api/
RUN dotnet publish FinancialMonitor.Api/FinancialMonitor.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage — small Alpine-based image (~85 MB)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
    && chown -R appuser:appgroup /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

USER appuser

HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -qO- http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "FinancialMonitor.Api.dll"]
