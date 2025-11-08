# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY ZatcaDotNet.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy from build stage
COPY --from=build /app/out ./

# Expose ports (ASP.NET Core default)
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "ZatcaDotNet.dll"]