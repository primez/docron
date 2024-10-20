FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
LABEL "org.opencontainers.image.description"="Docron is a scheduling tool designed to automate the start, stop and restart of Docker containers based on user-defined schedules. With a simple interface, it streamlines container management."
# Required for Time Zone database lookups
RUN apk add --no-cache tzdata

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Docron/Docron.csproj", "Docron/"]
RUN dotnet restore "Docron/Docron.csproj"
COPY . .
WORKDIR "/src/Docron"
RUN dotnet build "Docron.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Docron.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Cleanup step to remove all runtimes except for linux-x64
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS cleanup
WORKDIR /app
COPY --from=publish /app/publish ./
RUN find runtimes -mindepth 1 -maxdepth 1 ! -name 'linux-musl-x64' -exec rm -rf {} +

FROM base AS final
WORKDIR /app
COPY --from=cleanup /app .
ENTRYPOINT ["dotnet", "Docron.dll"]
