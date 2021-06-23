#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TurboYang.Tesla.Monitor.WebApi/TurboYang.Tesla.Monitor.WebApi.csproj", "TurboYang.Tesla.Monitor.WebApi/"]
COPY ["TurboYang.Tesla.Monitor.Core/TurboYang.Tesla.Monitor.Core.csproj", "TurboYang.Tesla.Monitor.Core/"]
RUN dotnet restore "TurboYang.Tesla.Monitor.WebApi/TurboYang.Tesla.Monitor.WebApi.csproj"
COPY . .
WORKDIR "/src/TurboYang.Tesla.Monitor.WebApi"
RUN dotnet build "TurboYang.Tesla.Monitor.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TurboYang.Tesla.Monitor.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TurboYang.Tesla.Monitor.WebApi.dll"]