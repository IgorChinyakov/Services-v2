FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/DirectoryService.Presentation/DirectoryService.Presentation.csproj", "src/DirectoryService.Presentation/"]
COPY ["src/DirectoryService.Infrastructure/DirectoryService.Infrastructure.csproj", "src/DirectoryService.Infrastructure/"]
COPY ["src/DirectoryService.Application/DirectoryService.Application.csproj", "src/DirectoryService.Application/"]
COPY ["src/DirectoryService.Contracts/DirectoryService.Contracts.csproj", "src/DirectoryService.Contracts/"]
COPY ["src/DirectoryService.Domain/DirectoryService.Domain.csproj", "src/DirectoryService.Domain/"]
RUN dotnet restore "src/DirectoryService.Presentation/DirectoryService.Presentation.csproj"
COPY . .
WORKDIR "/src/src/DirectoryService.Presentation"
RUN dotnet build "./DirectoryService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DirectoryService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DirectoryService.Presentation.dll"]
