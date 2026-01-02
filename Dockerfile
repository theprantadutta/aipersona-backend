FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and all project files
COPY ["aipersona-backend.sln", "./"]
COPY ["src/AiPersona.Api/AiPersona.Api.csproj", "src/AiPersona.Api/"]
COPY ["src/AiPersona.Application/AiPersona.Application.csproj", "src/AiPersona.Application/"]
COPY ["src/AiPersona.Domain/AiPersona.Domain.csproj", "src/AiPersona.Domain/"]
COPY ["src/AiPersona.Infrastructure/AiPersona.Infrastructure.csproj", "src/AiPersona.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "aipersona-backend.sln"

# Copy everything else
COPY . .

# Build the API project
WORKDIR "/src/src/AiPersona.Api"
RUN dotnet build "AiPersona.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AiPersona.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiPersona.Api.dll"]
