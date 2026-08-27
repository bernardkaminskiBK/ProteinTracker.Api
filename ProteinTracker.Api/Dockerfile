FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ProteinTracker.Api/ProteinTracker.Api.csproj ProteinTracker.Api/
RUN dotnet restore ProteinTracker.Api/ProteinTracker.Api.csproj

COPY ProteinTracker.Api/ ProteinTracker.Api/
RUN dotnet publish ProteinTracker.Api/ProteinTracker.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProteinTracker.Api.dll"]
