FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Docron.Ssr/Docron.Ssr.csproj", "Docron.Ssr/"]
RUN dotnet restore "Docron.Ssr/Docron.Ssr.csproj" --runtime linux-x64
COPY . .
WORKDIR "/src/Docron.Ssr"
RUN dotnet build "Docron.Ssr.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime linux-x64

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Docron.Ssr.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --runtime linux-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Docron.Ssr.dll"]
