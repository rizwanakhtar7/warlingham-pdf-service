FROM mcr.microsoft.com/playwright/dotnet:v1.59.0-jammy AS build

WORKDIR /src
COPY . .

RUN dotnet restore warlinghamcc.PdfService.csproj
RUN dotnet publish warlinghamcc.PdfService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/playwright/dotnet:v1.59.0-jammy

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "warlinghamcc.PdfService.dll"]
