# MicroMobilityHub

Plataforma de mapa colaborativo para infraestrutura de micromobilidade (bicicletários, estações de scooter, etc). Usuários podem descobrir, avaliar e comentar sobre bicicletários com filtros granulares para serviços, tipos de acesso e veículos suportados.

## Funcionalidades

### MVP (Fase 1)
- **Mapa interativo** com filtros de localização (raio 5-50km)
- **Operações CRUD** de bicicletários (colaborativo + moderação)
- **Sistema de avaliações** (1-5 estrelas + comentários)
- **Filtros granulares:**
  - Serviços: tomada, calibrador, armário, espaço de manutenção, cadeado próprio
  - Acesso: livre, pago, requer cadastro, mensal
  - Veículos: bicicleta, scooter, monociclo, patinete elétrico

### Fase 2
- Monetização freemium (operadores pagam por destaque/verificação)
- Sistema de reputação de usuários
- Notificações em tempo real para novos bicicletários próximos

## Arquitetura

**Arquitetura limpa em 4 camadas:**
- **Camada API** — Controllers, mapeamentos HTTP, middleware
- **Camada de Negócio** — Serviços, lógica de negócio, validações
- **Camada de Repositório** — EF Core DbContext, acesso a dados
- **Camada Compartilhada** — Modelos de domínio, DTOs, enums, exceções

## Stack Tecnológico

| Componente | Tecnologia |
|-----------|-----------|
| Backend | .NET 10 + C# + ASP.NET Core + EF Core |
| Banco de Dados | PostgreSQL 17 + PostGIS 3.5 (geoespacial) |
| Frontend | Blazor WebAssembly / PWA |
| Infraestrutura | Docker, GitHub Actions |
| ORM | Entity Framework Core 10 |

## Estrutura de Pastas

```
MobilityCenter/
├── src/
│   ├── MobilityCenter.API/              # API ASP.NET Core
│   ├── MobilityCenter.Business/         # Serviços e lógica de negócio
│   ├── MobilityCenter.Repositories/     # EF Core e acesso a dados
│   └── MobilityCenter.Shared/           # Modelos, DTOs, enums
├── docker-compose.yml                   # PostgreSQL + PostGIS
├── MobilityCenter.slnx                  # Arquivo de solução
└── CLAUDE.md                            # Guia de desenvolvimento para Claude Code
```

## Começando

### Pré-requisitos
- .NET 10 SDK
- Docker e Docker Compose
- Git

### Setup Local

1. **Clone o repositório**
   ```bash
   git clone https://github.com/heitormvl/mobilitycenter.git
   cd mobilitycenter
   ```

2. **Inicie PostgreSQL + PostGIS**
   ```bash
   docker compose up -d
   ```
   
   Banco estará disponível em:
   - Host: `localhost`
   - Porta: `5432`
   - Banco: `mobilitycenter`
   - Usuário: `mc_user`
   - Senha: `mc_dev_password`

3. **Restaure as dependências**
   ```bash
   dotnet restore
   ```

4. **Compile a solução**
   ```bash
   dotnet build
   ```

5. **Execute as migrações** (quando necessário)
   ```bash
   dotnet ef migrations add InitialCreate -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
   dotnet ef database update -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
   ```

6. **Inicie a API**
   ```bash
   dotnet run --project ./src/MobilityCenter.API
   ```
   
   API estará disponível em:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:7000`

## Desenvolvimento

### Comandos Comuns

```bash
# Build e execução
dotnet build
dotnet build -c Release
dotnet run --project ./src/MobilityCenter.API
dotnet watch run --project ./src/MobilityCenter.API

# Testes
dotnet test
dotnet test --filter "ClassName.MethodName"

# Formatação
dotnet format

# Banco de dados
dotnet ef migrations add MigrationName -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
dotnet ef database update -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
dotnet ef database drop -p ./src/MobilityCenter.Repositories -s ./src/MobilityCenter.API
```

### Configuração de CORS

**Desenvolvimento** (`appsettings.Development.json`):
- Permite requisições de `http://localhost:3000`, `http://localhost:4200`, `http://localhost:5173`, `https://localhost:7001`
- Permite qualquer header e método
- Credenciais habilitadas

**Produção** (`appsettings.Production.json`):
- Whitelist de origens específicas (configurar via variáveis de ambiente)
- Restrito aos headers necessários: `Content-Type`, `Authorization`
- Métodos permitidos: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`

### Configuração de Ambiente

As configurações são carregadas nesta ordem (valores posteriores sobrescrevem anteriores):
1. `appsettings.json` (base)
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (dev/prod)
3. Variáveis de ambiente (CI/CD, Docker)

**Desenvolvimento:** String de conexão e segredo JWT são commitados (valores padrão seguros).
**Produção:** Todos os valores sensíveis devem vir de variáveis de ambiente ou Azure Key Vault.

## Schema do Banco de Dados

### Modelos Principais

**Bicicletario** (Bicicletário)
- `Id` (Guid)
- `Name` (string)
- `Latitude`, `Longitude` (decimal)
- `Location` (PostGIS Point, SRID 4326)
- Flags de serviços: `HasPowerOutlet`, `HasAirPump`, `HasLocker`, `HasStorage`, `HasMaintenanceSpace`, `HasBikeLock`
- Tipo de acesso: `IsFree`, `IsPaid`, `RequiresSignup`, `IsMonthlySubscription`
- `VehicleTypes` (enum flags)
- `OperatorId` (FK para Usuario)
- `Ratings` (coleção de Avaliacao)
- `CreatedAt`, `UpdatedAt`, `IsDeleted`

**Usuario** (Usuário)
- `Id` (Guid)
- `Name` (string)
- `Email` (string, único)
- `UserType` (enum: Usuario, Operador, Admin)
- `CreatedAt`, `IsActive`

**Avaliacao** (Avaliação)
- `Id` (Guid)
- `BicicletarioId` (FK)
- `UsuarioId` (FK)
- `Rating` (1-5)
- `Comment` (string, opcional)
- `CreatedAt`

## Autenticação

Autenticação baseada em JWT (configurada em `appsettings.{environment}.json`):
- Issuer: `MobilityCenter`
- Audience: `MobilityCenter`
- Secret: Deve ser definido via variável de ambiente em produção

## Contribuindo

1. Crie uma branch de feature: `git checkout -b feature/minha-feature`
2. Commit as mudanças: `git commit -m "descrição"`
3. Push para o repositório remoto: `git push origin feature/minha-feature`
4. Crie um pull request

## Licença

[Especifique sua licença aqui]

## Autor

[Seu nome/contato]
