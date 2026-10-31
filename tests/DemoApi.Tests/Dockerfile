FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/DemoApi.Tests/DemoApi.Tests.csproj", "tests/DemoApi.Tests/"]
COPY ["src/DemoApi/DemoApi.csproj", "src/DemoApi/"]
RUN dotnet restore "tests/DemoApi.Tests/DemoApi.Tests.csproj"
COPY . .
WORKDIR "/src/tests/DemoApi.Tests"
RUN dotnet build "./DemoApi.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DemoApi.Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DemoApi.Tests.dll"]
