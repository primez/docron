FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
LABEL "org.opencontainers.image.description"="Docron is a scheduling tool designed to automate the start, stop and restart of Docker containers based on user-defined schedules. With a simple interface, it streamlines container management."
# Required for Time Zone database lookups
RUN apk add --no-cache tzdata

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Docron.Api/Docron.Api.csproj", "Docron.Api/"]
RUN dotnet restore "Docron.Api/Docron.Api.csproj"
COPY . .
WORKDIR "/src/Docron.Api"
RUN dotnet build "Docron.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Docron.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Docron.Api.dll"]
