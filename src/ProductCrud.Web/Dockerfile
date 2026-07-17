# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ProductCrud.Web/ProductCrud.Web.csproj ProductCrud.Web/
RUN dotnet restore ProductCrud.Web/ProductCrud.Web.csproj
COPY src/ProductCrud.Web/ ProductCrud.Web/
RUN dotnet publish ProductCrud.Web/ProductCrud.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductCrud.Web.dll"]
