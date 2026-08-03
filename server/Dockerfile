# Multi-stage build cho GameTeam.Api (docs/backend/solution-structure.md §5, docs/deployment/).
# BOOTSTRAP: host tối thiểu (chưa có API nghiệp vụ). Build context = repo root.
ARG DOTNET_VERSION=9.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY global.json ./
COPY server/Directory.Build.props server/Directory.Packages.props server/
COPY server/src/ server/src/
RUN dotnet restore server/src/GameTeam.Api/GameTeam.Api.csproj
RUN dotnet publish server/src/GameTeam.Api/GameTeam.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GameTeam.Api.dll"]
