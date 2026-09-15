# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TmsApi.Api/TmsApi.Api.csproj TmsApi.Api/
COPY TmsApi.Application/TmsApi.Application.csproj TmsApi.Application/
COPY TmsApi.Domain/TmsApi.Domain.csproj TmsApi.Domain/
COPY TmsApi.Infrastructure/TmsApi.Infrastructure.csproj TmsApi.Infrastructure/
RUN dotnet restore TmsApi.Api/TmsApi.Api.csproj
COPY . .
RUN dotnet publish TmsApi.Api/TmsApi.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
COPY --from=build /app/publish .
EXPOSE 10000
ENTRYPOINT ["dotnet", "TmsApi.Api.dll"]
