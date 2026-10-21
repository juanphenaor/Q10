FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Inventory/Inventory.slnx ./Inventory/
COPY Inventory/Inventory.Api/Inventory.Api.csproj ./Inventory/Inventory.Api/
COPY Inventory/Inventory.Application/Inventory.Application.csproj ./Inventory/Inventory.Application/
COPY Inventory/Inventory.Domain/Inventory.Domain.csproj ./Inventory/Inventory.Domain/
COPY Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj ./Inventory/Inventory.Infrastructure/

RUN dotnet restore ./Inventory/Inventory.slnx

COPY Inventory/ ./Inventory/

RUN dotnet publish ./Inventory/Inventory.Api/Inventory.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
