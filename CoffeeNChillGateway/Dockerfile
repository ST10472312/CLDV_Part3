
# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the project file and restore dependencies
COPY ["CoffeeNChillGateway/CoffeeNChillGateway.csproj", "CoffeeNChillGateway/"]

RUN dotnet restore "CoffeeNChillGateway/CoffeeNChillGateway.csproj"

# Copy the remaining source code
COPY . .

WORKDIR /src/CoffeeNChillGateway

# Publish the application
RUN dotnet publish "CoffeeNChillGateway.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "CoffeeNChillGateway.dll"]
