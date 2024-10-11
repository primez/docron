# Use the ASP.NET Core Runtime image as the base
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
LABEL "org.opencontainers.image.description"="Docron is a scheduling tool designed to automate the start and stop of Docker containers based on user-defined schedules. With a simple interface, it streamlines container management."

# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Docron.Server/Docron.Server.csproj", "Docron.Server/"]
RUN dotnet restore "Docron.Server/Docron.Server.csproj"
COPY . .
WORKDIR "/src/Docron.Server"
RUN dotnet build "Docron.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Docron.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Use the base image for the final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Docron.Server.dll"]