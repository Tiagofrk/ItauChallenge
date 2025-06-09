# Itau Challenge - API Financeira

## Descrição

Este projeto é uma API backend desenvolvida para gerenciar operações financeiras relacionadas a ativos, clientes e corretagem. Ele fornece endpoints para buscar cotações de ativos, posições de clientes, calcular preços médios e recuperar ganhos de corretagem. Esta API faz parte de um desafio técnico.

## Funcionalidades

- **Gestão de Ativos:**
  - Obter a cotação mais recente para um ativo específico.
- **Carteira de Clientes:**
  - Recuperar posições de investimento detalhadas para um cliente.
  - Identificar principais clientes com base no valor total da carteira (placeholder).
  - Identificar principais clientes com base nas taxas de corretagem (placeholder).
- **Operações de Usuário:**
  - Calcular o preço médio ponderado de compra de um ativo para um usuário.
- **Corretagem:**
  - Obter ganhos de corretagem acumulados durante um período especificado (placeholder).

## Começando

### Pré-requisitos

- .NET SDK 8.0
- Git

### Instalação e Configuração

1. Clone o repositório:
   ```bash
   git clone https://github.com/your-username/ItauChallenge.git
   ```
2. Navegue para o diretório do projeto:
   ```bash
   cd ItauChallenge
   ```
3. Restaure as dependências (a partir da raiz da solução ou do diretório do projeto da API):
   ```bash
   dotnet restore ItauChallenge.sln
   ```

### Executando a Aplicação

1. Navegue para o diretório do projeto da API:
   ```bash
   cd src/ItauChallenge.Api
   ```
2. Execute a aplicação:
   ```bash
   dotnet run
   ```
   A API deverá estar disponível em `https://localhost:<porta>` ou `http://localhost:<porta>` (verifique o arquivo `Properties/launchSettings.json` no projeto da API para a porta exata).

## Documentação da API

A documentação detalhada da API está disponível através da especificação OpenAPI.

- **OpenAPI JSON:** [src/ItauChallenge.Api/openapi.json](src/ItauChallenge.Api/openapi.json)
- **Swagger UI:** Com a aplicação em execução, a interface Swagger UI geralmente está disponível no endpoint `/swagger` (ex: `https://localhost:<porta>/swagger`).

## Executando Testes

Navegue até o diretório raiz da solução ou o diretório específico do projeto de teste e execute:

```bash
dotnet test ItauChallenge.sln
```

Ou para um projeto de teste específico:
```bash
cd test/ItauChallenge.Domain.Tests # Exemplo
dotnet test
```

## Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para enviar um pull request ou abrir uma issue. (Placeholder - pode ser expandido posteriormente com diretrizes mais específicas).

## Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE.txt](LICENSE.txt) para detalhes.

## Running with Docker

This section describes how to run the application and database using Docker and Docker Compose.

### Prerequisites

- Make sure you have Docker Desktop (or Docker Engine with Docker Compose) installed on your system.

### Environment Setup

1.  Navigate to the `deployment` directory:
    ```bash
    cd deployment
    ```
2.  Create a `.env` file in this directory (`deployment/.env`). This file will store the database password.
3.  Add the following line to the `.env` file, replacing `yourstrongpassword` with a secure password of your choice:
    ```
    DB_PASSWORD=yourstrongpassword
    ```
    **Note:** This password will be used for the `root` user of the MySQL database instance.

### How to Build and Run

1.  Navigate to the root directory of the project (the directory containing the `ItauChallenge.sln` file).
2.  Run the following command to build the Docker images and start the services (API and database) in detached mode:
    ```bash
    docker-compose -f deployment/docker-compose.yml up --build -d
    ```
    - The `-f deployment/docker-compose.yml` flag specifies the path to the Docker Compose file.
    - `--build` forces Docker to rebuild the images if there are any changes in the `Dockerfile` or the application code.
    - `-d` runs the containers in detached mode (in the background).

### Accessing the Application

-   **API:** Once the containers are running, the API should be accessible at `http://localhost:8080`.
-   **Database:** The MySQL database will be running on `localhost:3306`. You can connect to it using a database client with the following details:
    -   **Host:** `localhost`
    -   **Port:** `3306`
    -   **User:** `root`
    -   **Password:** The `DB_PASSWORD` you set in the `.env` file.
    -   **Database Name:** `itau_challenge_db`

### How to Stop

1.  Ensure you are in the project root directory.
2.  To stop and remove the containers, networks, and volumes created by `docker-compose`, run:
    ```bash
    docker-compose -f deployment/docker-compose.yml down
    ```
    If you want to remove the database data volume as well (all data will be lost), you can use:
    ```bash
    docker-compose -f deployment/docker-compose.yml down -v
    ```
