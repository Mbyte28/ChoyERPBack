FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/ChoyERP.Domain/ChoyERP.Domain.csproj src/ChoyERP.Domain/
COPY src/ChoyERP.Application/ChoyERP.Application.csproj src/ChoyERP.Application/
COPY src/ChoyERP.Infrastructure/ChoyERP.Infrastructure.csproj src/ChoyERP.Infrastructure/
COPY src/ChoyERP.Api/ChoyERP.Api.csproj src/ChoyERP.Api/

RUN dotnet restore src/ChoyERP.Api/ChoyERP.Api.csproj

COPY src/ src/

RUN dotnet publish src/ChoyERP.Api/ChoyERP.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "ChoyERP.Api.dll"]
