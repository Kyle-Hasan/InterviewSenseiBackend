# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Set environment to ensure the API listens on the desired port
ENV ASPNETCORE_URLS=http://+:8080

WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project and restore dependencies
COPY ["API/API.csproj", "API/"]
RUN dotnet restore "API/API.csproj"

# Copy everything and build
COPY . . 
WORKDIR "/src/API"
RUN dotnet build "API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

# Install dependencies early to improve caching
RUN apt update && apt install -y ffmpeg

# Ensure ffmpeg is available in /app/x64/
RUN mkdir -p /app/x64 && cp /usr/bin/ffmpeg /app/x64/ffmpeg

# Copy published app from build stage
COPY --from=publish /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "API.dll"]
