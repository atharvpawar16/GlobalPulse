FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish GlobalPulse.Api.csproj -c Release -o /app/publish --no-restore || \
    (dotnet restore GlobalPulse.Api.csproj && dotnet publish GlobalPulse.Api.csproj -c Release -o /app/publish)

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "GlobalPulse.Api.dll"]
