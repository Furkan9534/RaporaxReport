# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["AuthApi.csproj", "./"]
RUN dotnet restore "AuthApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "AuthApi.csproj" -c Release -o /app/build

# 2. Publish Stage
FROM build AS publish
RUN dotnet publish "AuthApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose standard port for .NET 8 containers
EXPOSE 8080

# Configure ASP.NET to run on port 8080 (default for .NET 8)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AuthApi.dll"]
