# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/Tequio.Api/Tequio.Api.csproj", "backend/Tequio.Api/"]
COPY ["backend/Tequio.Domain/Tequio.Domain.csproj", "backend/Tequio.Domain/"]
COPY ["backend/Tequio.Infrastructure/Tequio.Infrastructure.csproj", "backend/Tequio.Infrastructure/"]

RUN dotnet restore "backend/Tequio.Api/Tequio.Api.csproj"

COPY backend/ backend/
WORKDIR "/src/backend/Tequio.Api"
RUN dotnet publish "Tequio.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tequio.Api.dll"]