FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Orders/Orders.slnx ./Orders/
COPY Orders/Orders.Api/Orders.Api.csproj ./Orders/Orders.Api/
COPY Orders/Orders.Application/Orders.Application.csproj ./Orders/Orders.Application/
COPY Orders/Orders.Domain/Orders.Domain.csproj ./Orders/Orders.Domain/
COPY Orders/Orders.Infrastructure/Orders.Infrastructure.csproj ./Orders/Orders.Infrastructure/
COPY Tests/Backend.Tests/Backend.Tests.csproj ./Tests/Backend.Tests/

RUN dotnet restore ./Orders/Orders.slnx

COPY Orders/ ./Orders/

RUN dotnet publish ./Orders/Orders.Api/Orders.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Orders.Api.dll"]
