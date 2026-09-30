FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/ ./src/
RUN dotnet publish src/TheOne/TheOne.API.csproj -c Release -o /out /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production DataProtection__KeysPath=/app/data-protection-keys
RUN mkdir -p /app/data-protection-keys && chown -R app:app /app/data-protection-keys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TheOne.API.dll"]
