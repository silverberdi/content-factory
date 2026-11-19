FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/api/ContentFactory.Api/ContentFactory.Api.csproj", "src/api/ContentFactory.Api/"]
RUN dotnet restore "src/api/ContentFactory.Api/ContentFactory.Api.csproj"

COPY src/api/ContentFactory.Api/ src/api/ContentFactory.Api/
WORKDIR "/src/src/api/ContentFactory.Api"
RUN dotnet publish "ContentFactory.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ContentFactory.Api.dll"]
