# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_HTTPS_PORT=8081
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=password
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/devcert.pfx

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["gateway/Gateway.API/Gateway.API.csproj", "gateway/Gateway.API/"]
RUN dotnet restore "./gateway/Gateway.API/Gateway.API.csproj"
COPY . .
WORKDIR "/src/gateway/Gateway.API"
RUN dotnet build "./Gateway.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY gateway/Gateway.API/appsettings.*.json .
ENTRYPOINT ["dotnet", "Gateway.API.dll"]