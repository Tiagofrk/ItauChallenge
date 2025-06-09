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
   A API deverá estar disponível em `https://localhost:<porta>` ou `http://localhost:<porta>` (verifique o arquivo `Properties/launchSettings.json` no projeto da API para a porta exata). Para testar interativamente os endpoints da API, acesse a interface do Swagger UI, que geralmente é aberta automaticamente ou pode ser acessada adicionando `/swagger` à URL base da aplicação (por exemplo, `http://localhost:<porta>/swagger`).

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

## Escalabilidade e Balanceamento de Carga

Com o crescimento do sistema para 1 milhão de operações/dia, é crucial implementar estratégias de escalabilidade e balanceamento de carga para garantir a disponibilidade e o desempenho do serviço.

### Auto-scaling Horizontal

O auto-scaling horizontal ajusta automaticamente o número de instâncias do serviço em execução com base na demanda. Para aplicar o auto-scaling horizontal no serviço, podemos seguir estas etapas:

1.  **Conteinerização:** Empacotar a aplicação em contêineres Docker. Isso já está parcialmente configurado no projeto com o `Dockerfile` e `docker-compose.yml`.
2.  **Orquestração de Contêineres:** Utilizar uma plataforma de orquestração de contêineres como Kubernetes (K8s) ou AWS Elastic Container Service (ECS). Essas plataformas gerenciam o ciclo de vida dos contêineres, incluindo a escalabilidade.
3.  **Definição de Métricas de Escalabilidade:** Configurar métricas que dispararão o auto-scaling. As métricas comuns incluem:
    *   **Uso de CPU:** Aumentar o número de instâncias quando o uso médio de CPU exceder um limite (por exemplo, 70%).
    *   **Uso de Memória:** Escalar quando o consumo de memória atingir um patamar crítico.
    *   **Número de Requisições por Segundo (RPS):** Adicionar mais instâncias se o número de requisições por instância ultrapassar um valor definido.
    *   **Latência da Aplicação:** Escalar se a latência média das respostas aumentar além de um threshold aceitável.
    *   **Métricas Personalizadas:** Dependendo da natureza do serviço, métricas específicas do negócio (por exemplo, tamanho de filas de processamento) podem ser usadas.
4.  **Configuração do Auto-scaler:**
    *   **No Kubernetes:** Utilizar o Horizontal Pod Autoscaler (HPA). O HPA monitora as métricas definidas e ajusta o número de pods (instâncias da aplicação) no deployment. É necessário ter um Metrics Server configurado no cluster.
    *   **Em Provedores de Nuvem (AWS, Azure, GCP):**
        *   **AWS:** Utilizar o Application Auto Scaling com ECS ou EC2 Auto Scaling Groups se estiver executando em instâncias EC2.
        *   **Azure:** Utilizar o Azure Autoscale para Virtual Machine Scale Sets ou Azure App Service.
        *   **GCP:** Utilizar o Autoscaler para Managed Instance Groups (MIGs) ou Google Kubernetes Engine (GKE).
5.  **Testes e Monitoramento:** Após a configuração, realizar testes de carga para verificar se o auto-scaling está funcionando conforme o esperado e monitorar continuamente o comportamento do sistema em produção.

### Comparação de Estratégias de Balanceamento de Carga

O balanceador de carga distribui o tráfego de entrada entre as várias instâncias do serviço, melhorando a disponibilidade e a eficiência. Duas estratégias comuns são:

#### Round-Robin

*   **Como Funciona:** Distribui as requisições sequencialmente para cada servidor disponível na lista. Se houver três servidores (A, B, C), a primeira requisição vai para A, a segunda para B, a terceira para C, a quarta para A novamente, e assim por diante.
*   **Prós:**
    *   Simples de implementar e entender.
    *   Distribuição uniforme do tráfego, supondo que todos os servidores tenham capacidade similar.
*   **Contras:**
    *   Não leva em consideração a carga atual ou a saúde de cada servidor. Um servidor sobrecarregado ou lento continuará recebendo requisições na sua vez.
    *   Pode levar a um desempenho degradado se houver disparidade na capacidade ou no estado dos servidores.
*   **Quando Usar:** Ideal para cenários onde os servidores têm capacidades homogêneas e as cargas de trabalho são relativamente uniformes e previsíveis.

#### Latência (Least Response Time)

*   **Como Funciona:** O balanceador de carga envia a requisição para o servidor que está respondendo mais rapidamente no momento (menor latência). Alguns sistemas também podem combinar isso com o menor número de conexões ativas (Least Connections).
*   **Prós:**
    *   Adapta-se dinamicamente às condições da rede e à carga dos servidores.
    *   Pode melhorar significativamente o tempo de resposta percebido pelo usuário, pois as requisições são direcionadas aos servidores mais ágeis.
    *   Leva em consideração a saúde e a performance atual de cada servidor.
*   **Contras:**
    *   Mais complexo de implementar, pois requer monitoramento contínuo da latência de cada servidor.
    *   Pode, em alguns casos, sobrecarregar os servidores mais rápidos se não houver um mecanismo para evitar que recebam todas as novas requisições após uma recuperação rápida.
    *   O cálculo da latência pode adicionar um pequeno overhead.
*   **Quando Usar:** Preferível para aplicações onde o tempo de resposta é crítico e onde a carga nos servidores ou as condições da rede podem variar significativamente. É uma boa escolha para garantir uma experiência de usuário mais consistente e rápida.

Considerando um volume de 1 milhão de operações/dia, uma estratégia baseada em **latência** (ou uma combinação como Least Connections + Fastest Response Time) é geralmente mais adequada, pois otimiza a performance e a resiliência, direcionando o tráfego para as instâncias que podem processá-lo mais eficientemente no momento.

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

Após a execução bem-sucedida dos contêineres, a API estará acessível em `http://localhost:8080` (HTTP) e `https://localhost:8081` (HTTPS).
A interface do Swagger UI para interagir e testar a API estará disponível em `http://localhost:8080/swagger` ou `https://localhost:8081/swagger`.

## Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para enviar um pull request ou abrir uma issue. (Placeholder - pode ser expandido posteriormente com diretrizes mais específicas).

## Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE.txt](LICENSE.txt) para detalhes.
