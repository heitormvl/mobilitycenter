FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/MobilityCenter.Shared/MobilityCenter.Shared.csproj             src/MobilityCenter.Shared/
COPY src/MobilityCenter.Repositories/MobilityCenter.Repositories.csproj src/MobilityCenter.Repositories/
COPY src/MobilityCenter.Business/MobilityCenter.Business.csproj         src/MobilityCenter.Business/
COPY src/MobilityCenter.API/MobilityCenter.API.csproj                   src/MobilityCenter.API/
RUN dotnet restore src/MobilityCenter.API/MobilityCenter.API.csproj

COPY src/ src/
RUN dotnet publish src/MobilityCenter.API/MobilityCenter.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MobilityCenter.API.dll"]
