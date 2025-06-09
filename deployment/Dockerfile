# Estágio 1: Build da aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos .csproj e restaura as dependências
COPY ["src/ItauChallenge.Api/ItauChallenge.Api.csproj", "ItauChallenge.Api/"]
COPY ["src/ItauChallenge.Domain/ItauChallenge.Domain.csproj", "ItauChallenge.Domain/"]
COPY ["src/ItauChallenge.Infra/ItauChallenge.Infra.csproj", "ItauChallenge.Infra/"]
RUN dotnet restore "ItauChallenge.Api/ItauChallenge.Api.csproj"

# Copia todo o código fonte e publica a aplicação
# WORKDIR /src # Already in /src
COPY . .
WORKDIR "/src/src/ItauChallenge.Api"
RUN dotnet publish "ItauChallenge.Api.csproj" -c Release -o /app/publish

# Estágio 2: Execução da aplicação
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Copia o script do banco para dentro da imagem final, no caminho que o C# espera
COPY ["src/ItauChallenge.Infra/scripts.txt", "scripts.txt"]

ENTRYPOINT ["dotnet", "ItauChallenge.Api.dll"]
