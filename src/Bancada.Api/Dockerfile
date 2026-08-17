FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY ["src/Bancada.Api/Bancada.Api.csproj", "src/Bancada.Api/"]
COPY ["src/Bancada.Application/Bancada.Application.csproj", "src/Bancada.Application/"]
COPY ["src/Bancada.Domain/Bancada.Domain.csproj", "src/Bancada.Domain/"]
COPY ["src/Bancada.Infrastructure/Bancada.Infrastructure.csproj", "src/Bancada.Infrastructure/"]
RUN dotnet restore "src/Bancada.Api/Bancada.Api.csproj"

COPY . .
RUN dotnet publish "src/Bancada.Api/Bancada.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Bancada.Api.dll"]
