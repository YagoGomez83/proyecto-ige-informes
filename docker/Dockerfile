FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY IGE.Informes.sln ./
COPY src/IGE.Informes.Domain/IGE.Informes.Domain.csproj src/IGE.Informes.Domain/
COPY src/IGE.Informes.Application/IGE.Informes.Application.csproj src/IGE.Informes.Application/
COPY src/IGE.Informes.Infrastructure/IGE.Informes.Infrastructure.csproj src/IGE.Informes.Infrastructure/
COPY src/IGE.Informes.Web/IGE.Informes.Web.csproj src/IGE.Informes.Web/
COPY tests/IGE.Informes.UnitTests/IGE.Informes.UnitTests.csproj tests/IGE.Informes.UnitTests/
COPY tests/IGE.Informes.IntegrationTests/IGE.Informes.IntegrationTests.csproj tests/IGE.Informes.IntegrationTests/

RUN dotnet restore src/IGE.Informes.Web/IGE.Informes.Web.csproj

COPY src/ src/

RUN dotnet publish src/IGE.Informes.Web/IGE.Informes.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r ige && useradd -r -g ige ige
RUN mkdir -p /keys /app/generated && chown ige:ige /keys /app/generated
USER ige

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IGE.Informes.Web.dll"]
