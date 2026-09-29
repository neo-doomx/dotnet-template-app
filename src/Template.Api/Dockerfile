# Build context is the repository root: docker build -f src/Template.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project or package version changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Template.Api/Template.Api.csproj src/Template.Api/
COPY src/Template.Application/Template.Application.csproj src/Template.Application/
COPY src/Template.Domain/Template.Domain.csproj src/Template.Domain/
COPY src/Template.Infrastructure/Template.Infrastructure.csproj src/Template.Infrastructure/
RUN dotnet restore src/Template.Api/Template.Api.csproj

COPY src/ src/
RUN dotnet publish src/Template.Api/Template.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Template.Api.dll"]
