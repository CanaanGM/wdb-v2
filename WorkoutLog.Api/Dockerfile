FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WorkoutLog.Api/WorkoutLog.Api.csproj", "WorkoutLog.Api/"]
RUN dotnet restore "WorkoutLog.Api/WorkoutLog.Api.csproj"
COPY . .
WORKDIR "/src/WorkoutLog.Api"
RUN dotnet build "./WorkoutLog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WorkoutLog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkoutLog.Api.dll"]
