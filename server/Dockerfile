FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Novashelf.Api/Novashelf.Api.csproj Novashelf.Api/
RUN dotnet restore Novashelf.Api/Novashelf.Api.csproj
COPY Novashelf.Api/ Novashelf.Api/
RUN dotnet publish Novashelf.Api/Novashelf.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Novashelf.Api.dll"]
