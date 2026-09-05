# Place this file at: LTSFrontend/Dockerfile
# Adjust the .csproj filename/path below to match your actual project structure.

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN useradd --uid 1000 appuser
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "LTSFrontend.dll"]
