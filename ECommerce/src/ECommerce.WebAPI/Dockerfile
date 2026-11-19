# Build context: repo kökü (Directory.Build.props, BannedSymbols.txt ve Uludag.Core orada).
#   docker build -f ECommerce/src/ECommerce.WebAPI/Dockerfile -t uludag-ecommerce-api .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Önce sadece proje dosyaları -> restore katmanı kod değişmedikçe cache'ten gelir.
COPY Directory.Build.props BannedSymbols.txt ./
COPY Uludag.Core/Uludag.Core.csproj Uludag.Core/
COPY ECommerce/src/ECommerce.Domain/ECommerce.Domain.csproj ECommerce/src/ECommerce.Domain/
COPY ECommerce/src/ECommerce.Application/ECommerce.Application.csproj ECommerce/src/ECommerce.Application/
COPY ECommerce/src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj ECommerce/src/ECommerce.Infrastructure/
COPY ECommerce/src/ECommerce.Persistence/ECommerce.Persistence.csproj ECommerce/src/ECommerce.Persistence/
COPY ECommerce/src/ECommerce.WebAPI/ECommerce.WebAPI.csproj ECommerce/src/ECommerce.WebAPI/
RUN dotnet restore ECommerce/src/ECommerce.WebAPI/ECommerce.WebAPI.csproj

COPY Uludag.Core/ Uludag.Core/
COPY ECommerce/src/ ECommerce/src/
RUN dotnet publish ECommerce/src/ECommerce.WebAPI/ECommerce.WebAPI.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Root olmayan kullanıcı (aspnet imajında hazır gelir).
USER $APP_UID

# TokenOptions__SecurityKey imaja gömülmez; "docker run -e" ile verilir.
ENTRYPOINT ["dotnet", "ECommerce.WebAPI.dll"]
