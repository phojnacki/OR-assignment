FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/OR.Shared/OR.Shared.csproj", "src/OR.Shared/"]
COPY ["src/OR.InventoryService.Domain/OR.InventoryService.Domain.csproj", "src/OR.InventoryService.Domain/"]
COPY ["src/OR.InventoryService.Application/OR.InventoryService.Application.csproj", "src/OR.InventoryService.Application/"]
COPY ["src/OR.InventoryService.Infrastructure/OR.InventoryService.Infrastructure.csproj", "src/OR.InventoryService.Infrastructure/"]
COPY ["src/OR.InventoryService.Api/OR.InventoryService.Api.csproj", "src/OR.InventoryService.Api/"]
RUN dotnet restore "src/OR.InventoryService.Api/OR.InventoryService.Api.csproj"

COPY src/ src/
WORKDIR /src/src/OR.InventoryService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OR.InventoryService.Api.dll"]
