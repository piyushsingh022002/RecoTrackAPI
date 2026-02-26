# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and restore dependencies
COPY ProjectRecoTrack.sln ./
COPY RecoTrackApi/*.csproj ./RecoTrackApi/
COPY RecoTrack.Application/*.csproj ./RecoTrack.Application/
COPY RecoTrack.Infrastructure/*.csproj ./RecoTrack.Infrastructure/
COPY RecoTrackApi.ControllersTest/*.csproj ./RecoTrackApi.ControllersTest/
COPY RecoTrackApi.ServiceTests/*.csproj ./RecoTrackApi.ServiceTests/
COPY RecoTrack.Data/*.csproj ./RecoTrack.Data/
COPY RecoTrack.Shared/*.csproj ./RecoTrack.Shared/
RUN dotnet restore ProjectRecoTrack.sln


# Copy all source and build
COPY . .
RUN dotnet publish RecoTrackApi/RecoTrackApi.csproj -c Release -o /app/publish

# Runtime stage
# FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# WORKDIR /app
# COPY --from=build /app/publish .
# ENV ASPNETCORE_URLS=http://+:8080
# EXPOSE 8080
# ENTRYPOINT ["dotnet", "RecoTrackApi.dll"]


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user (important for Azure)
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RecoTrackApi.dll"]