FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies (cached layer)
COPY ["DandDTemplateParserCSharp/DandDTemplateParserCSharp.csproj", "DandDTemplateParserCSharp/"]
RUN dotnet restore "DandDTemplateParserCSharp/DandDTemplateParserCSharp.csproj"

# Build and publish
COPY . .
WORKDIR /src/DandDTemplateParserCSharp
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DandDTemplateParserCSharp.dll"]
