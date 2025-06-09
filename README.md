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

## Easter Eggs

Aqui detalhamos algumas das mensagens ocultas e seus significados:

### Imagem do Coelho Branco

A imagem de um coelho branco é uma clara alusão à expressão "seguir o coelho branco", popularizada pelo filme Matrix, que por sua vez se inspira em Alice no País das Maravilhas.

### Mensagem Hexadecimal e RLE

A mensagem em hexadecimal `4120756c74696d61206c696e686120657374c3a120636f6d206f2062696ec3a172696f20656d20524c45` é decodificada convertendo os valores hexadecimais para caracteres ASCII. O resultado é: "A ultima linha está com o binário em RLE". Isso indica que a próxima etapa de decodificação envolve a técnica de Run-Length Encoding.

### Decodificação Final do RLE

O resultado da decodificação desses dados é um fluxo de bits (zeros e uns) que, quando formatado corretamente, revela uma imagem em arte ASCII do coelho branco, que também é mostrado como uma imagem no documento.

## Executando com Docker

### Pré-requisitos

- É necessário ter o Docker Desktop instalado em seu sistema.

### Como Construir e Executar

1. Navegue até o diretório `C:\Projetos\ItauChallenge\deployment>`.
2. Execute o seguinte comando para construir as imagens Docker e iniciar os serviços:
   ```bash
   docker-compose up --build -d
   ```

## Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para enviar um pull request ou abrir uma issue. (Placeholder - pode ser expandido posteriormente com diretrizes mais específicas).

## Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE.txt](LICENSE.txt) para detalhes.
