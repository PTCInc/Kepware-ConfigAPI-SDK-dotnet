# Learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging at https://aka.ms/customizecontainer

# These ARGs allow switching the base used to create the final image when debugging from VS.
ARG LAUNCHING_FROM_VS
# This sets the base image for 'final', but only if LAUNCHING_FROM_VS is defined.
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# This stage is used when running from VS in Fast mode (Default for Debug configuration).
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Install clang/zlib1g-dev dependencies for native AOT publishing
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["KepwareSync.Service/Kepware.SyncService.csproj", "KepwareSync.Service/"]
COPY ["Kepware.Api/Kepware.Api.csproj", "Kepware.Api/"]
RUN dotnet restore "./KepwareSync.Service/Kepware.SyncService.csproj"
COPY . .
WORKDIR "/src/KepwareSync.Service"
RUN dotnet build "./Kepware.SyncService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Kepware.SyncService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

# This stage is used as the base for the final stage when launched from VS to support debugging in regular mode (Default when not using Debug configuration).
FROM base AS aotdebug
USER root
# Install GDB to support native debugging.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gdb
USER app

# This stage is used in production or when running from VS in regular mode (Default when not using Debug configuration).
FROM ${FINAL_BASE_IMAGE:-mcr.microsoft.com/dotnet/runtime-deps:10.0} AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./Kepware.SyncService"]