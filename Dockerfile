# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/RealEstateTax.Domain/RealEstateTax.Domain.csproj",          "src/RealEstateTax.Domain/"]
COPY ["src/RealEstateTax.Application/RealEstateTax.Application.csproj","src/RealEstateTax.Application/"]
COPY ["src/RealEstateTax.Infrastructure/RealEstateTax.Infrastructure.csproj","src/RealEstateTax.Infrastructure/"]
COPY ["src/RealEstateTax.Intelligence/RealEstateTax.Intelligence.csproj","src/RealEstateTax.Intelligence/"]
COPY ["src/RealEstateTax.API/RealEstateTax.API.csproj",                "src/RealEstateTax.API/"]

RUN dotnet restore "src/RealEstateTax.API/RealEstateTax.API.csproj"

COPY . .

RUN dotnet build "src/RealEstateTax.API/RealEstateTax.API.csproj" -c Release -o /app/build

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Publish
# ──────────────────────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "src/RealEstateTax.API/RealEstateTax.API.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ──────────────────────────────────────────────────────────────────────────────
# Stage 3: Runtime
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/retax/uploads && chmod 755 /var/retax/uploads

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RealEstateTax.API.dll"]
