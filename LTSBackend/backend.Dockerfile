# Place this file at: LTSBackend/Dockerfile
# Adjust the .csproj filename/path below to match your actual project structure.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the csproj first for better layer caching
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Non-root user for a bit of extra safety
RUN useradd --uid 1000 appuser
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "LTSBackend.dll"]
