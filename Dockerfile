FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Paraki.Shared/Paraki.Shared.csproj             src/Paraki.Shared/
COPY src/Paraki.Repositories/Paraki.Repositories.csproj src/Paraki.Repositories/
COPY src/Paraki.Business/Paraki.Business.csproj         src/Paraki.Business/
COPY src/Paraki.API/Paraki.API.csproj                   src/Paraki.API/
RUN dotnet restore src/Paraki.API/Paraki.API.csproj

COPY src/ src/
RUN dotnet publish src/Paraki.API/Paraki.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Paraki.API.dll"]
