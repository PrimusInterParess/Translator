FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj first for better layer caching
COPY translator-proxy.csproj ./
RUN dotnet restore ./translator-proxy.csproj

# copy the rest and publish
COPY . ./
RUN dotnet publish ./translator-proxy.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV PORT=8788
EXPOSE 8788

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "translator-proxy.dll"]
