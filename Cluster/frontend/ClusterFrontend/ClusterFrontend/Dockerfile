# See https://aka.ms/customizecontainer to learn how to customize your debug container

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Optional if you define APP_UID externally
# USER $APP_UID
WORKDIR /app

# Support both HTTP and HTTPS
EXPOSE 5002
EXPOSE 5003

ENV ASPNETCORE_URLS=http://+:5002;https://+:5003
ENV ASPNETCORE_HTTPS_PORT=5003
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=password
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/devcert.pfx

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["frontend/ClusterFrontend/ClusterFrontend/ClusterFrontend.csproj", "frontend/ClusterFrontend/ClusterFrontend/"]
COPY ["frontend/ClusterFrontend/ClusterFrontend.Client/ClusterFrontend.Client.csproj", "frontend/ClusterFrontend/ClusterFrontend.Client/"]
RUN dotnet restore "./frontend/ClusterFrontend/ClusterFrontend/ClusterFrontend.csproj"
COPY . .
WORKDIR "/src/frontend/ClusterFrontend/ClusterFrontend"
RUN dotnet build "./ClusterFrontend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ClusterFrontend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClusterFrontend.dll"]
