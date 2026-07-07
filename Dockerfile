FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Reserva.Domain/Reserva.Domain.csproj Reserva.Domain/
COPY Reserva.Infrastructure/Reserva.Infrastructure.csproj Reserva.Infrastructure/
COPY Reserva.Web/Reserva.Web.csproj Reserva.Web/

RUN dotnet restore Reserva.Web/Reserva.Web.csproj

COPY . .

RUN dotnet publish Reserva.Web/Reserva.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Reserva.Web.dll"]
