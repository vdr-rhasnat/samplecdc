#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 8081
EXPOSE 4433

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Heartbeat.csproj", "."]
RUN dotnet restore "./Heartbeat.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Heartbeat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Heartbeat.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Heartbeat.dll"]