# Multi-stage: big SDK image builds, small runtime image ships.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Prophix.Api/Prophix.Api.csproj src/Prophix.Api/
RUN dotnet restore src/Prophix.Api/Prophix.Api.csproj    # cached layer unless csproj changes
COPY src/ src/
RUN dotnet publish src/Prophix.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Prophix.Api.dll"]
