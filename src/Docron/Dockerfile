# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
# Install necessary dependencies in a single RUN statement to leverage Docker layer caching
RUN apk add \
    clang \
    binutils \
    musl-dev \
    build-base \
    zlib-static \
    cmake \
    openssl-dev \
    openssl-libs-static \
    openssl
# Set the working directory
WORKDIR /app
# Copy project files
COPY Docron/Docron.csproj Docron/
# Restore dependencies for the Docron project
RUN dotnet restore --runtime linux-musl-x64 Docron/Docron.csproj
# Copy the entire source code
COPY . .
# Set working directory to the Docron project
WORKDIR /app/Docron
# Publish the application in Release mode for linux-musl-x64 runtime without the app host
RUN dotnet publish Docron.csproj -c Release -r linux-musl-x64 -o out

# Stage 2: Create the runtime image using scratch for minimal size
FROM scratch AS runtime
WORKDIR /app
COPY --from=build /app/Docron/out/ /app/
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080
ENTRYPOINT ["/app/Docron"]