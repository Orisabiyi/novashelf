# The Novashelf API. Build from the server/ directory:
#   docker build -t novashelf-api .
# Azure Container Apps builds this in the cloud, so Docker isn't needed locally.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Novashelf.Api/Novashelf.Api.csproj Novashelf.Api/
RUN dotnet restore Novashelf.Api/Novashelf.Api.csproj
COPY Novashelf.Api/ Novashelf.Api/
RUN dotnet publish Novashelf.Api/Novashelf.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# The image's built-in non-root user; the API listens on 8080.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Novashelf.Api.dll"]
