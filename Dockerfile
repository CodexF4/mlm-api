# syntax=docker/dockerfile:1

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (layer-cached until the csproj changes).
COPY mlm.csproj ./
RUN dotnet restore

# Build and publish.
COPY . ./
RUN dotnet publish mlm.csproj -c Release -o /app /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_ENVIRONMENT=Production
# Render injects PORT at runtime; the app reads it and binds to 0.0.0.0:$PORT.
EXPOSE 8080

ENTRYPOINT ["dotnet", "mlm.dll"]
