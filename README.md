# MicroMobilityHub

Plataforma de mapa colaborativo para infraestrutura de micromobilidade (bicicletários, estações de scooter, etc). Usuários podem descobrir, avaliar e comentar sobre bicicletários com filtros granulares para serviços, tipos de acesso e veículos suportados.

## Funcionalidades

### MVP (Fase 1) — Em Desenvolvimento
- ✅ **Mapa interativo** com renderização de localização
- ✅ **Listagem de bicicletários** com filtragem básica
- ✅ **Tela de detalhes** com informações completas, avaliações e comentários
- ✅ **Criação de bicicletários** em 3 steps (localização → informações → serviços)
- ✅ **Sistema de avaliações** (1-5 estrelas + comentários)
- ✅ **Filtros granulares:**
  - Serviços: tomada, calibrador, armário, espaço de manutenção, cadeado próprio
  - Acesso: livre, pago, requer cadastro, mensal
  - Veículos: bicicleta, scooter, monociclo, patinete elétrico
- 🔄 **Autenticação e Perfil** (páginas criadas, integração em progresso)

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
│   ├── MobilityCenter.API/              # API ASP.NET Core (Controllers, Middleware)
│   ├── MobilityCenter.Business/         # Serviços e lógica de negócio
│   ├── MobilityCenter.Repositories/     # EF Core e acesso a dados
│   ├── MobilityCenter.Shared/           # Modelos, DTOs, enums
│   └── MobilityCenter.Frontend/         # Blazor WebAssembly
│       ├── Pages/                       # Páginas: Mapa, Lista, Detalhe, Adicionar (Step1-3), Avaliar, Login, Perfil
│       ├── Components/                  # Componentes reutilizáveis: BottomNav, Buttons, Stars, Chip, etc.
│       └── Services/                    # Serviços HTTP e lógica do cliente
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

7. **Inicie o Frontend (Blazor WASM)** (em outra janela do terminal)
   ```bash
   dotnet run --project ./src/MobilityCenter.Frontend
   ```
   
   Frontend estará disponível em:
   - `http://localhost:5173` ou `https://localhost:7001` (dependendo da configuração)

## Frontend (Blazor WebAssembly)

### Páginas Principais
- **Mapa** (`/mapa`) — Mapa interativo com localização de bicicletários
- **Lista** (`/lista`) — Listagem de bicicletários com filtros
- **Detalhe** (`/bicicletario/{id}`) — Informações completas, avaliações e comentários
- **Adicionar** (`/adicionar`) — Multi-step form para criar novo bicicletário (3 steps)
- **Avaliar** (`/avaliar/{id}`) — Interface para adicionar avaliação/comentário
- **Login** (`/login`) — Autenticação de usuários
- **Perfil** (`/perfil`) — Dados do usuário e histórico

### Componentes Reutilizáveis
- `BottomNav` — Navegação inferior (móvel)
- `BtnP` / `BtnO` — Botões primário e outline
- `Stars` — Componente de avaliação por estrelas
- `Chip` — Tags/labels para filtros e categorias
- `CheckItem` / `RadioItem` — Itens de lista com seleção
- `WfInput` — Input com estilo customizado
- `MapGrid` — Grid para exibição de bicicletários

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

## Integração Frontend-API

### Status Atual
- ✅ Listagem de bicicletários integrada com API
- ✅ Detalhes de bicicletário e avaliações
- ✅ Criação de bicicletários via formulário multi-step
- ✅ DTOs alinhados entre frontend e backend (nomes em português)
- 🔄 Autenticação e Perfil (em progresso)
- 🔄 Filtros avançados (em progresso)

### Próximas Etapas
- Implementar autenticação JWT completa no frontend
- Integrar filtros de serviços e tipos de acesso
- Melhorar tratamento de erros e validações
- Implementar cache e otimizações de performance
- Adicionar PWA features (offline mode, instalação)

## Autenticação

Autenticação baseada em JWT (configurada em `appsettings.{environment}.json`):
- Issuer: `MobilityCenter`
- Audience: `MobilityCenter`
- Secret: Deve ser definido via variável de ambiente em produção

Frontend utiliza JWT token armazenado em localStorage para requisições autenticadas.

## Status do Projeto

🚀 **Em desenvolvimento ativo** — MVP em construção com foco em:
1. Consolidar integração frontend-backend
2. Implementar autenticação completa
3. Adicionar filtros avançados
4. Testes de integração
5. Deploy em ambiente de staging

## Contribuindo

1. Crie uma branch de feature: `git checkout -b feature/minha-feature`
2. Commit as mudanças: `git commit -m "feat/fix: descrição clara"`
3. Push para o repositório remoto: `git push origin feature/minha-feature`
4. Crie um pull request com descrição detalhada

### Dicas de Desenvolvimento
- Use o arquivo `CLAUDE.md` para entender a arquitetura
- Execute `dotnet format` antes de commits
- Crie migrations incrementais para alterações de banco
- Teste endpoints com Swagger UI em desenvolvimento

## Licença

[Especifique sua licença aqui]

## Autor

Hector Vieira (heitormvl@gmail.com)
