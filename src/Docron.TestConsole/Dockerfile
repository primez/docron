FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Docron.TestConsole/Docron.TestConsole.csproj", "Docron.TestConsole/"]
RUN dotnet restore "Docron.TestConsole/Docron.TestConsole.csproj"
COPY . .
WORKDIR "/src/Docron.TestConsole"
RUN dotnet build "Docron.TestConsole.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Docron.TestConsole.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Docron.TestConsole.dll"]
