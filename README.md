# Paraki

Plataforma de mapa colaborativo para infraestrutura de micromobilidade (bicicletários, estações de scooter, etc). Usuários podem descobrir, avaliar e comentar sobre bicicletários com filtros granulares para serviços, tipos de acesso e veículos suportados.

## Funcionalidades

### MVP (Fase 1) — Em Desenvolvimento Ativo
- ✅ **Mapa interativo** com renderização de localização, popup compacto horizontal
- ✅ **Listagem de bicicletários** com filtragem granular em aside lateral (mobile: bottom sheet)
- ✅ **Tela de detalhes** (responsiva mobile/PC) com informações completas, avaliações, comentários e edição de avaliações
- ✅ **Criação de bicicletários** em 3 steps (localização → informações → serviços, layout responsivo)
- ✅ **Sistema de avaliações** (1-5 estrelas + comentários com edição)
- ✅ **Upload de fotos** para bicicletários e perfil de usuário
- ✅ **Filtros granulares:**
  - Serviços: tomada, calibrador, armário, espaço de manutenção, cadeado próprio, banheiro
  - Acesso: livre, pago, requer cadastro, mensal
  - Veículos: bicicleta, scooter, monociclo, patinete elétrico
- ✅ **Autenticação JWT** (login, registro, esqueci senha, confirmar email)
- ✅ **Perfil de usuário** (informações, foto, histórico de avaliações)
- ✅ **Sugestões de edição** para correção colaborativa de informações
- ✅ **Layout responsivo mobile/PC** com sidebar colapsável e grid de 2 colunas
- ✅ **Google AdSense** (Auto Ads integrado)

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
Paraki/
├── src/
│   ├── Paraki.API/              # API ASP.NET Core (Controllers, Middleware)
│   ├── Paraki.Business/         # Serviços e lógica de negócio
│   ├── Paraki.Repositories/     # EF Core e acesso a dados
│   ├── Paraki.Shared/           # Modelos, DTOs, enums
│   └── Paraki.Frontend/         # Blazor WebAssembly
│       ├── Pages/                       # Páginas: Mapa, Lista, Detalhe, Adicionar (Step1-3), Avaliar, Login, Perfil
│       ├── Components/                  # Componentes reutilizáveis: BottomNav, Buttons, Stars, Chip, etc.
│       └── Services/                    # Serviços HTTP e lógica do cliente
├── docker-compose.yml                   # PostgreSQL + PostGIS
├── Paraki.slnx                  # Arquivo de solução
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
   git clone https://github.com/heitormvl/paraki.git
   cd paraki
   ```

2. **Inicie PostgreSQL + PostGIS**
   ```bash
   docker compose up -d
   ```
   
   Banco estará disponível em:
   - Host: `localhost`
   - Porta: `5432`
   - Banco: `paraki`
   - Usuário: `mc_user`
   - Senha: `mc_dev_password`

3. **Instale as ferramentas globais do EF Core**
   ```bash
   dotnet tool install --global dotnet-ef
   ```

4. **Restaure as dependências**
   ```bash
   dotnet restore
   ```

5. **Compile a solução**
   ```bash
   dotnet build
   ```

6. **Execute as migrações** (quando necessário)
   ```bash
   dotnet ef migrations add InitialCreate -p ./src/Paraki.Repositories -s ./src/Paraki.API
   dotnet ef database update -p ./src/Paraki.Repositories -s ./src/Paraki.API
   ```

7. **Inicie a API**
   ```bash
   dotnet run --project ./src/Paraki.API
   ```
   
   API estará disponível em:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:7000`

8. **Inicie o Frontend (Blazor WASM)** (em outra janela do terminal)
   ```bash
   dotnet run --project ./src/Paraki.Frontend
   ```
   
   Frontend estará disponível em:
   - `http://localhost:5173` ou `https://localhost:7001` (dependendo da configuração)

## Frontend (Blazor WebAssembly)

### Páginas Principais
- **Mapa** (`/mapa`) — Mapa interativo com sidebar colapsável (PC) e localização de bicicletários
- **Lista** (`/lista`) — Listagem de bicicletários com filtros em aside lateral ou bottom sheet (mobile)
- **Detalhe** (`/bicicletario/{id}`) — Layout responsivo mobile/PC com foto principal, grid de fotos, avaliações e comentários editáveis
- **Adicionar** (`/adicionar`) — Multi-step form responsivo para criar novo bicicletário (3 steps: localização, informações, serviços)
- **Avaliar** (`/avaliar/{id}`) — Interface otimizada para adicionar/editar avaliação e comentário
- **Login** (`/login`) — Autenticação JWT com email e senha
- **Registro** (`/registrar`) — Criação de nova conta com validação de email
- **Esqueci Senha** (`/esqueci-senha`) — Recuperação de senha via email
- **Redefinir Senha** (`/redefinir-senha`) — Redefinição de senha com token
- **Confirmar Email** (`/confirmar-email`) — Confirmação de email para ativar conta
- **Perfil** (`/perfil`) — Layout 2 colunas (PC) com dados do usuário, foto, histórico de avaliações e configurações
- **Configurações** (`/configuracoes`) — Configurações de conta, notificações e privacidade
- **Política de Privacidade** (`/politica-privacidade`) — Informações de privacidade e termos

### Componentes Reutilizáveis
- `BottomNav` — Navegação inferior (tabs: Mapa, Lista, Adicionar, Perfil)
- `SideNav` — Navegação lateral com drawer para PC
- `BtnP` / `BtnO` — Botões primário e outline com variações de tamanho
- `Stars` — Componente de avaliação por estrelas (leitura e edição)
- `Chip` — Tags/labels para filtros e categorias com seleção
- `CheckItem` / `RadioItem` — Itens de lista com seleção
- `WfInput` — Input com estilo customizado e validação
- `MapGrid` — Grid responsivo para exibição de bicicletários
- `Toast` — Notificações flutuantes (sucesso, erro, info)
- `ConfirmModal` — Modal de confirmação para ações destrutivas
- `ConfiguracoesSheet` — Bottom sheet para configurações rápidas
- `ImgPH` — Placeholder de imagem com fallback
- `StatusBar` — Barra de status para exibir estado da aplicação
- `ProgressBar` — Barra de progresso para multi-step forms
- `SecLabel` — Label secundário para campos de formulário

## Desenvolvimento

### Comandos Comuns

```bash
# Build e execução
dotnet build
dotnet build -c Release
dotnet run --project ./src/Paraki.API
dotnet watch run --project ./src/Paraki.API
dotnet run --project ./src/Paraki.Frontend

# Testes unitários e integração
dotnet test
dotnet test --filter "ClassName.MethodName"

# Testes E2E (Playwright)
dotnet test ./src/Paraki.E2E.Tests
dotnet test ./src/Paraki.E2E.Tests --logger "html"  # Gerar relatório HTML

# Formatação e verificação de código
dotnet format
dotnet format --verify-no-changes

# Banco de dados
dotnet ef migrations add MigrationName -p ./src/Paraki.Repositories -s ./src/Paraki.API
dotnet ef database update -p ./src/Paraki.Repositories -s ./src/Paraki.API
dotnet ef database drop -p ./src/Paraki.Repositories -s ./src/Paraki.API
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
- Flags de serviços: `HasPowerOutlet`, `HasAirPump`, `HasLocker`, `HasStorage`, `HasMaintenanceSpace`, `HasBikeLock`, `HasRestroom`
- Tipo de acesso: `IsFree`, `IsPaid`, `RequiresSignup`, `IsMonthlySubscription`
- `VehicleTypes` (enum flags)
- Horários de funcionamento: `HorariosOperacaoJson` (JSON com dias/horas)
- `OperatorId` (FK para Usuario)
- `Ratings` (coleção de Avaliacao)
- `Fotos` (coleção de Foto)
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
- `CreatedAt`, `UpdatedAt`

**Foto** (Foto de Bicicletário ou Perfil)
- `Id` (Guid)
- `BicicletarioId` (FK, opcional para fotos de perfil)
- `UsuarioId` (FK)
- `StorageUrl` (URL no storage externo)
- `ThumbnailUrl` (versão miniaturizada)
- `CreatedAt`

**SugestaoEdicao** (Sugestão de Edição Colaborativa)
- `Id` (Guid)
- `BicicletarioId` (FK)
- `UsuarioId` (FK — quem sugeriu)
- `Tipo` (enum: ServiçoAdicionado, ServiçoRemovido, InformacaoCorrigida, etc.)
- `Descricao` (string)
- `Status` (enum: Pendente, Aprovada, Rejeitada)
- `Aprovadoem` (datetime, nullable)
- `CreatedAt`

**RefreshToken** (Token de Renovação)
- `Id` (Guid)
- `UsuarioId` (FK)
- `Token` (string, único)
- `ExpiresAt` (datetime)
- `CreatedAt`

## Integração Frontend-API

### Status Atual
- ✅ Listagem e busca de bicicletários com cache e lazy loading
- ✅ Detalhes de bicicletário com avaliações, comentários e edição
- ✅ Criação de bicicletários via formulário multi-step (3 steps)
- ✅ Filtros granulares integrados (serviços, acesso, veículos)
- ✅ Autenticação JWT completa (login, registro, refresh tokens)
- ✅ Perfil de usuário com histórico de avaliações
- ✅ Upload de fotos (bicicletário e perfil)
- ✅ Sugestões de edição para correção colaborativa
- ✅ DTOs alinhados entre frontend e backend (nomes em português)
- ✅ Layout responsivo mobile/PC com componentes adaptativos

### Próximas Etapas
- Implementar monetização freemium (destaque/verificação de operadores)
- Notificações em tempo real para novos bicicletários
- Sistema de reputação de usuários
- PWA features completas (offline mode, instalação, push notifications)
- Testes E2E expansivos

## Autenticação

Autenticação baseada em JWT (configurada em `appsettings.{environment}.json`):
- Issuer: `Paraki`
- Audience: `Paraki`
- Secret: Deve ser definido via variável de ambiente em produção
- Refresh Tokens: Suportados para renovação automática de sessão

**Frontend:**
- JWT token armazenado em localStorage para requisições autenticadas
- Refresh token storage em httpOnly cookies (seguro contra XSS)
- Interceptor HTTP para renovação automática de token

## Otimizações e Recursos

### Performance
- **Cache de Localização:** Armazena última localização do usuário para melhorar UX
- **Lazy Loading:** Bicicletários e fotos carregam sob demanda
- **Projeção SQL:** Queries otimizadas com seleção de apenas campos necessários
- **Fingerprinting:** Assets estáticos com hash automático para cache-busting
- **Índices PostGIS:** Queries geoespaciais otimizadas

### Assets e SEO
- **Sitemap XML:** Gerado automaticamente para indexação
- **Google AdSense:** Integrado para monetização
- **Lazy Loading de Imagens:** Blur-up effect com LQIP (Low Quality Image Placeholder)
- **Responsive Images:** Srcset automático para diferentes resoluções

### Interoperabilidade
- **JavaScript Interop:** MapInterop.js para integração com Mapbox/Leaflet
- **WebGL Rendering:** Mapa com renderização acelerada

## Status do Projeto

🚀 **Em desenvolvimento ativo** — MVP praticamente completo com foco em:

### Concluído (Fase 1 — MVP)
- ✅ Integração frontend-backend completa
- ✅ Autenticação JWT com refresh tokens
- ✅ Filtros granulares implementados
- ✅ Layout responsivo mobile/PC
- ✅ Upload de fotos com storage
- ✅ Sistema de avaliações com edição
- ✅ Sugestões de edição colaborativas
- ✅ Google AdSense integrado
- ✅ Testes E2E com Playwright

### Em Progresso / Próximas Etapas
1. Expansão de testes E2E (cobertura de 100%)
2. Otimizações de performance (caching avançado, lazy loading)
3. PWA features (offline mode, push notifications)
4. Monetização freemium (destaque de locais)
5. Sistema de reputação de usuários
6. Notificações em tempo real (SignalR)
7. Deploy em staging/produção

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
