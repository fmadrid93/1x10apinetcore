# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solución y proyectos
COPY ["ApiEncuestaNetCore.sln", "./"]
COPY ["ApiWeb/ApiWeb.csproj", "ApiWeb/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Dtos/Dtos.csproj", "Dtos/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "ApiWeb/ApiWeb.csproj"

# Copiar todo el código fuente
COPY . .

# Publicar
WORKDIR "/src/ApiWeb"
RUN dotnet publish -c Release -o /app/out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "ApiWeb.dll"]
