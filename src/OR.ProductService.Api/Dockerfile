FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["src/OR.Shared/OR.Shared.csproj", "src/OR.Shared/"]
COPY ["src/OR.ProductService.Domain/OR.ProductService.Domain.csproj", "src/OR.ProductService.Domain/"]
COPY ["src/OR.ProductService.Application/OR.ProductService.Application.csproj", "src/OR.ProductService.Application/"]
COPY ["src/OR.ProductService.Infrastructure/OR.ProductService.Infrastructure.csproj", "src/OR.ProductService.Infrastructure/"]
COPY ["src/OR.ProductService.Api/OR.ProductService.Api.csproj", "src/OR.ProductService.Api/"]
RUN dotnet restore "src/OR.ProductService.Api/OR.ProductService.Api.csproj"

COPY src/ src/
WORKDIR /src/src/OR.ProductService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OR.ProductService.Api.dll"]
