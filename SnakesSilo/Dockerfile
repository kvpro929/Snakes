#See https://aka.ms/containerfastmode to understand how Visual Studio uses ths Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
EXPOSE 11111
EXPOSE 30000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS publish
ARG GIT_SHA
WORKDIR /src
COPY ["../SnakesSilo/SnakesSilo.csproj", "SnakesSilo/"]
COPY ["../GrainInterfaces/GrainInterfaces.csproj", "GrainInterfaces/"]
COPY ["../SharedTypes/SharedTypes.csproj", "SharedTypes/"]
COPY ["../Grains/Grains.csproj", "Grains/"]
RUN dotnet restore "SnakesSilo/SnakesSilo.csproj"
COPY . .
WORKDIR "/src/SnakesSilo"
RUN dotnet publish --no-restore "SnakesSilo.csproj" -c Release -o /app/publish /p:GitSha=${GIT_SHA}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnakesSilo.dll"]