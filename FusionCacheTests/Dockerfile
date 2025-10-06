# =====================================================
# Etapa 1: Build
# =====================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /src

COPY . .

RUN dotnet restore ./FusionCacheTests.csproj \
    --runtime linux-musl-x64 \
    /p:PublishReadyToRun=true

# =====================================================
# Etapa 2: Publicação
# =====================================================
FROM build AS publish

RUN dotnet publish ./FusionCacheTests.csproj \
    --configuration Release \
    --output /publish \
    --no-restore \
    --runtime linux-musl-x64 \
    /p:PublishReadyToRun=true \
    /p:PublishTrimmed=true \
    /p:TieredCompilation=true \
    /p:DebugType=None \
    /p:DebugSymbols=false \
    /p:PublishSingleFile=false \
    /p:EnableCompressionInSingleFile=false \
    /p:IncludeAllLocalesForSelfContained=false \
    /p:Optimize=true \
    /p:PublishCrossgen2=true \
    /p:PublishContainer

# =====================================================
# Etapa Final: Imagem de Runtime
# =====================================================
FROM jacksonveroneze/aspnet:9.0-alpine AS runtime

ENV ASSEMBLY_NAME=FusionCacheTests.dll

COPY --from=publish /publish .
