# Bancada

Bancada é uma comunidade de receitas para quem cozinha em casa. O MVP reúne publicação e descoberta de receitas, perfis de cozinheiros, favoritos, comentários, desafios culinários e uma Caixa Misteriosa de ingredientes.

## Funcionalidades

- conta, sessão por cookie HTTP-only e perfil público;
- criação, edição, exclusão, publicação e foto de receitas;
- feed paginado e exploração por texto, dificuldade, tempo e ingrediente;
- favoritos e comentários simples;
- desafios ativos ou encerrados, com participação por receita;
- Caixa Misteriosa sem persistência ou serviços de IA;
- dados de desenvolvimento com receitas e desafios brasileiros plausíveis;
- estados de carregamento, vazio e erro em uma interface responsiva em português.

## Arquitetura e stack

A solution usa .NET 10 e mantém cinco responsabilidades diretas:

- `Bancada.Web`: cliente Blazor WebAssembly independente;
- `Bancada.Api`: Minimal APIs, Identity, autorização, OpenAPI e composição;
- `Bancada.Domain`: entidades e regras centrais;
- `Bancada.Application`: contratos HTTP e a abstração pequena de arquivos;
- `Bancada.Infrastructure`: EF Core, Npgsql, Identity, PostgreSQL e storages local/R2.

O cliente conversa com a API por REST. O banco de produção pode ser um PostgreSQL do Neon sem código específico do provedor. Imagens usam filesystem no desenvolvimento e a API compatível com S3 do Cloudflare R2 em produção. As decisões que merecem contexto adicional estão em [docs/architecture.md](docs/architecture.md).

## Requisitos locais

- SDK do .NET 10;
- PostgreSQL acessível localmente ou uma conexão de desenvolvimento no Neon;
- HTTPS development certificate confiável: `dotnet dev-certs https --trust`.

Não há credenciais no repositório. Configure a conexão local com user secrets:

```powershell
dotnet user-secrets init --project src/Bancada.Api
dotnet user-secrets set "ConnectionStrings:Bancada" "Host=localhost;Port=5432;Database=bancada;Username=postgres;Password=SUA_SENHA" --project src/Bancada.Api
```

Em ambientes hospedados, use `ConnectionStrings__Bancada`. O arquivo `.env.example` relaciona todas as variáveis aceitas, mas não é carregado automaticamente pela aplicação.

## Banco e execução

Restaure as ferramentas e aplique a migration inicial:

```powershell
dotnet tool restore
dotnet ef database update --project src/Bancada.Infrastructure --startup-project src/Bancada.Api
```

Inicie a API e o cliente em terminais separados:

```powershell
dotnet run --project src/Bancada.Api
dotnet run --project src/Bancada.Web
```

Por padrão, a API usa `https://localhost:7262` e o cliente `https://localhost:7139`. Em `Development`, a API aplica migrations pendentes e cria dez receitas, dois desafios e contas locais de demonstração. O cliente pode ser apontado para outra API em `src/Bancada.Web/wwwroot/appsettings.json`; o CORS correspondente fica em `Client:Origin` na API.

A especificação OpenAPI fica em `/openapi/v1.json` e a verificação de saúde em `/health`.

## Imagens e Cloudflare R2

O provider padrão é `Local`, com arquivos em `src/Bancada.Api/wwwroot/uploads`. Para R2, defina:

```text
Storage__Provider=R2
Storage__R2__AccountId=...
Storage__R2__AccessKeyId=...
Storage__R2__SecretAccessKey=...
Storage__R2__BucketName=bancada-images
Storage__R2__PublicBaseUrl=https://images.seu-dominio.com
```

`PublicBaseUrl` deve ser um domínio público ligado ao bucket. A aplicação valida configuração, MIME type, extensão e limite de 5 MB, e gera o nome do objeto sem reutilizar o nome enviado pelo navegador.

## Testes e qualidade

```powershell
dotnet restore Bancada.sln
dotnet build Bancada.sln --configuration Release --no-restore
dotnet test Bancada.sln --configuration Release --no-build
dotnet list Bancada.sln package --vulnerable --include-transitive
```

Os testes de domínio cobrem regras importantes. Os testes de API usam SQLite relacional isolado e exercitam o pipeline real de autenticação, autorização, receitas, favoritos e desafios sem depender de credenciais externas.

## Deploy

Publique `Bancada.Web` e envie o conteúdo de `artifacts/web/wwwroot` para uma hospedagem estática:

```powershell
dotnet publish src/Bancada.Web --configuration Release --output artifacts/web
```

A API possui um Dockerfile para build a partir da raiz do repositório:

```powershell
docker build --file src/Bancada.Api/Dockerfile --tag bancada-api .
```

Configure no host a conexão do Neon, a origem HTTPS do frontend e as variáveis do R2. A migration deve ser aplicada como uma etapa controlada do deploy antes de iniciar a nova versão da API. O workflow em `.github/workflows/ci.yml` executa restore, build e testes em pushes e pull requests para `main`.

## Fora do MVP

Seguidores, mensagens, notificações, rankings, pagamentos e receitas geradas por IA ficam fora do escopo atual. Só devem entrar quando houver uma necessidade de produto concreta.
