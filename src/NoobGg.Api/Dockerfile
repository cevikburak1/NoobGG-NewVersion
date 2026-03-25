FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NoobGg.Api/NoobGg.Api.csproj", "NoobGg.Api/"]
COPY ["NoobGg.Application/NoobGg.Application.csproj", "NoobGg.Application/"]
COPY ["NoobGg.Domain/NoobGg.Domain.csproj", "NoobGg.Domain/"]
COPY ["NoobGg.Infrastructure/NoobGg.Infrastructure.csproj", "NoobGg.Infrastructure/"]
RUN dotnet restore "NoobGg.Api/NoobGg.Api.csproj"
COPY . .
RUN dotnet build "NoobGg.Api/NoobGg.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoobGg.Api/NoobGg.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoobGg.Api.dll"]
