# EventFlow

**Aqui é um projeto dedicado ao meu estudo mesmo , aplicando todos os conhecimentos que o backend me proporciona e evoluindo cada vez mais!!**

API de gestão de eventos e ingressos em ASP.NET Core 10, com autenticação JWT em cookie `HttpOnly`, sessão revogável, autorização por perfil, banco **PostgreSQL** (via Docker), jobs em background com **Hangfire** e envio de e-mail real via SMTP.

Projeto de estudo — o passo a passo completo da construção está no [blueprint](https://claude.ai/code/artifact/bc81f614-f44b-4450-839e-233d96922e20).

---

## Requisitos

| | Versão | Como obter |
|---|---|---|
| .NET SDK | **10.0** ou superior | https://dotnet.microsoft.com/download |
| Docker Desktop | qualquer versão recente | https://www.docker.com/products/docker-desktop |

O banco não é mais um arquivo local: o projeto usa **PostgreSQL rodando em container Docker**. Isso é obrigatório mesmo se você rodar a API fora do Docker — não existe mais opção "sem instalar nada", porque o Hangfire (que gerencia os jobs em background) não tem storage oficial para SQLite.

A ferramenta `dotnet-ef` só é necessária se você for **alterar o modelo** e gerar novas migrations:

```bash
dotnet tool install --global dotnet-ef
```

Confira o SDK:

```bash
dotnet --version
```

---

## Rodando

### 1. Clonar e restaurar

```bash
git clone <url-do-repositorio>
cd EventFlow
dotnet restore
```

O `restore` baixa todas as dependências listadas no `.csproj`. Não é preciso instalar pacote nenhum à mão.

### 2. Configurar os segredos

Quatro segredos, nenhum vai para o Git: a chave JWT, o `ClientSecret` do Google, e agora `EMAIL_FROM`/`EMAIL_APP_PASSWORD` (conta usada para mandar e-mail de confirmação de compra).

**Onde cada segredo é lido depende de como você roda a aplicação:**

| Rodando via | Onde configurar | Por quê |
|---|---|---|
| `docker compose up` (recomendado) | arquivo `.env` na raiz | o container é isolado — não enxerga nada da sua máquina, `.env` é o jeito do Docker Compose injetar variável de ambiente pra dentro dele |
| `dotnet run` direto na máquina | User Secrets (`dotnet user-secrets set ...`) | roda no seu processo local, que sabe ler o `secrets.json` guardado fora do repositório |

#### 2.1 Chave JWT

**Obrigatório.** A chave que assina os tokens não está no repositório — quem a tem consegue forjar um token dizendo ser qualquer usuário, com qualquer perfil.

**Via `.env`** (copie `.env.example` para `.env` e preencha):
```env
JWT_KEY=<gere com o comando abaixo>
```

Gerar a chave — **Windows (PowerShell)**:
```powershell
$b = New-Object byte[] 32
([System.Security.Cryptography.RNGCryptoServiceProvider]::new()).GetBytes($b)
[Convert]::ToBase64String($b)
```
**Linux / macOS**:
```bash
openssl rand -base64 32
```

**Via User Secrets** (se for rodar sem Docker):
```bash
cd EventFlow
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<chave-gerada>"
```

> A chave é gerada, nunca digitada. Uma frase como `"minha-chave-super-secreta"` tem entropia baixíssima — é palavra de dicionário, está no topo de qualquer lista de tentativas.
>
> Cada pessoa gera a própria chave. Elas **não** precisam ser iguais: quem assina e quem valida é a mesma aplicação, na mesma máquina.

#### 2.2 Login com Google (opcional)

**Diferente da chave JWT, isso não pode ser gerado localmente.** `ClientId` e `ClientSecret` são credenciais emitidas pelo Google, atreladas a um projeto cadastrado no Google Cloud.

1. Acesse [console.cloud.google.com](https://console.cloud.google.com) e crie um projeto (ou use um existente)
2. Vá em **Google Auth Platform → Público-alvo**, escolha **Externo**, preencha nome do app e email de suporte, e adicione seu email como **usuário de teste**
3. Vá em **Clientes → Criar um cliente OAuth**, tipo **Aplicativo da Web**
4. Em **URIs de redirecionamento autorizados**, adicione a URL correspondente a como você vai rodar (veja passo 3 e 4 abaixo):
   ```
   http://localhost:8080/api/auth/google/signin-callback   (via Docker)
   https://localhost:7076/api/auth/google/signin-callback  (local, sem Docker)
   ```
   Precisa bater **exatamente** com o `CallbackPath` configurado no `Program.cs`
5. Copie o **ID do cliente** e a **Chave secreta do cliente** gerados

O `ClientId` não é segredo — vai no `appsettings.json`:
```json
"Authentication": {
  "Google": {
    "ClientId": "SEU-CLIENT-ID.apps.googleusercontent.com"
  }
}
```

O `ClientSecret` é segredo:
```env
# .env
GOOGLE_CLIENT_SECRET=SEU-CLIENT-SECRET
```
```bash
# ou, sem Docker:
dotnet user-secrets set "Authentication:Google:ClientSecret" "SEU-CLIENT-SECRET"
```

> Sem isso configurado, a aplicação recusa subir: `GoogleAuthOptions` é validado no arranque (`ValidateOnStart`), igual ao `JwtOptions`. Se você só quer testar o login local (email/senha), ainda assim precisa desses dois valores presentes (mesmo vazios não passa na validação `[Required]`) — deixe o cadastro no Google feito antes de rodar.

#### 2.3 Envio de e-mail (Gmail SMTP)

**Obrigatório.** É usado para mandar o e-mail de confirmação de compra de ingresso via MailKit.

1. Acesse [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords) (precisa ter verificação em duas etapas ativada na conta)
2. Gere uma **senha de app** — não é a senha normal da conta, é uma credencial extra, revogável a qualquer momento sem afetar o login
3. Configure:
```env
# .env
EMAIL_FROM=seu-email@gmail.com
EMAIL_APP_PASSWORD=<senha-de-app-de-16-caracteres>
```
```bash
# ou, sem Docker:
dotnet user-secrets set "Email:From" "seu-email@gmail.com"
dotnet user-secrets set "Email:AppPassword" "<senha-de-app>"
```

> Assim como `JwtOptions`/`GoogleAuthOptions`, `EmailOptions` também é validado no arranque. Sem `Email:From`/`Email:AppPassword` configurados, a aplicação lança exceção ao subir — em Docker, combinado com `restart: on-failure`, isso vira um loop de reinício sem fim (ver [Solução de problemas](#solução-de-problemas)).

### 3. Subir com Docker Compose (recomendado)

Sobe a API **e** o Postgres juntos, já conectados entre si:

```bash
docker compose up -d --build
```

- `--build` é necessário sempre que o código, o `Dockerfile` ou o `.csproj` mudarem — sem ele o Docker reaproveita uma imagem antiga.
- Acompanhar os logs: `docker compose logs eventflow-api -f`
- Derrubar tudo: `docker compose down` (os dados do Postgres persistem no volume `postgres-data`; `docker compose down -v` também apaga o volume)

Na primeira subida o banco é criado e populado automaticamente (ver [DbSeeder](EventFlow/Infrastructure/Data/DbSeeder.cs)).

Abra: **http://localhost:8080/swagger**

Dashboard do Hangfire: **http://localhost:8080/hangfire** (usuário/senha configurados no `Program.cs` — ver seção [Background jobs](#background-jobs-com-hangfire)).

### 4. Alternativa: rodar localmente sem Docker

Precisa do Postgres rodando mesmo assim — só a API sai do container:

```bash
docker compose up -d postgres
cd EventFlow
dotnet run --launch-profile https
```

A connection string local (`appsettings.Development.json`) já aponta para `Host=localhost` (diferente da usada dentro do Docker, que é `Host=postgres` — nome do serviço, não `localhost`, porque containers se enxergam pelo nome do serviço na rede do Compose).

Abra: **https://localhost:7076/swagger**

#### Se o navegador reclamar do certificado (só no modo local)

O cookie de sessão usa `Secure = true`, então só trafega em HTTPS. Se aparecer *"Sua conexão não é particular"*, confie no certificado de desenvolvimento — uma vez por máquina:

```bash
dotnet dev-certs https --trust
```

---

## Usuários de exemplo

Na primeira execução o banco é criado e populado automaticamente. Não precisa rodar migrations nem cadastrar nada.

| Email | Senha | Perfil |
|---|---|---|
| `ana@teste.com` | `123456` | **Organizador** |
| `bruno@teste.com` | `123456` | **Participante** |

Também já existem: o evento *Rock in Rio*, o perfil de participante do Bruno (CPF `12345678901`) e um ingresso comprado por ele.

> Senhas fracas assim servem só para teste local. O que fica gravado no banco é o hash BCrypt, nunca a senha em texto.

### Como logar no Swagger

```json
POST /api/auth/login

{ "email": "ana@teste.com", "senha": "123456" }
```

A resposta é só `{"mensagem":"Login efetuado"}` — **parece que nada aconteceu, mas aconteceu**. O token foi para um cookie `HttpOnly`, invisível ao JavaScript e ao Swagger. O navegador passa a enviá-lo sozinho nas próximas chamadas.

Por isso o botão **Authorize** do Swagger não é usado aqui: ele serve para token no header `Authorization`, e o nosso viaja em cookie.

Para trocar de usuário, faça `POST /api/auth/logout` antes de logar com o outro.

---

## Testando as permissões

O roteiro que mostra a autorização funcionando:

1. Logue como **bruno@teste.com** (Participante)
2. `GET /api/evento` → **200**, ele pode ver eventos
3. `POST /api/evento` → **403**, ele não pode criar
4. `POST /api/auth/logout`
5. Logue como **ana@teste.com** (Organizador)
6. `POST /api/evento` → **201**

**401 vs 403:** `401` significa "não sei quem você é" (falta token ou expirou). `403` significa "sei quem você é, e você não pode". Se o Participante receber 401 no passo 3, o problema é o login, não a permissão.

---

## Endpoints

### Autenticação — `/api/auth`

| Método | Rota | Quem pode |
|---|---|---|
| POST | `/registrar` | qualquer um |
| POST | `/login` | qualquer um |
| GET | `/google` | qualquer um (redireciona pro login do Google) |
| GET | `/google/callback` | qualquer um (chamado pelo Google depois do consentimento) |
| POST | `/renovar` | quem tem sessão válida |
| POST | `/logout` | autenticado |

### Eventos — `/api/evento`

| Método | Rota | Quem pode |
|---|---|---|
| GET | `/` | autenticado |
| GET | `/{id}` | autenticado |
| POST | `/` | **Organizador** |
| PUT | `/{id}` | **Organizador** |
| PUT | `/{id}/inativar` | **Organizador** |

### Participantes — `/api/participante`

| Método | Rota | Quem pode |
|---|---|---|
| POST | `/` | autenticado (cria o próprio perfil) |
| GET | `/` | **Organizador** |
| GET | `/{id}` | autenticado |

### Ingressos — `/api/ingresso`

| Método | Rota | Quem pode |
|---|---|---|
| POST | `/comprar` | autenticado |
| POST | `/{id}/cancelar` | autenticado (só o próprio) |
| GET | `/meus` | autenticado |
| POST | `/checkin/{codigo}` | **Organizador** |
| GET | `/participante/{id}` | **Organizador** |

---

## Background jobs com Hangfire

Trabalho que não precisa travar a resposta HTTP roda em background, gerenciado pelo [Hangfire](https://www.hangfire.io/), com o próprio Postgres como storage (schema `hangfire`, separado das tabelas de negócio).

| Job | Tipo | Dispara quando |
|---|---|---|
| `DesativarEventosPassados` | Recurring (`Cron.Daily(0, 0)`) | todo dia à meia-noite — soft delete (`Ativo = false`) em eventos com `DataHora` no passado |
| `EnviarConfirmacaoCompraAsync` | Fire-and-forget | logo após `POST /api/ingresso/comprar` |

Dashboard: **`/hangfire`**, protegido por autenticação básica (usuário/senha definidos direto no `Program.cs` — troque antes de expor isso em qualquer lugar que não seja sua máquina de estudo).

---

## Envio de e-mail

Confirmação de compra de ingresso é mandada por e-mail de verdade, via Gmail SMTP + [MailKit](https://github.com/jstedfast/MailKit) — ver [`EmailService`](EventFlow/Infrastructure/Email/EmailService.cs). Configuração em [2.3](#23-envio-de-e-mail-gmail-smtp).

O envio acontece dentro do job `EnviarConfirmacaoCompraAsync` (fire-and-forget), não na resposta HTTP da compra — o e-mail chega alguns segundos depois, não instantaneamente.

---

## O banco de dados

O banco é **PostgreSQL**, rodando em container Docker (serviço `postgres` no `docker-compose.yml`), com os dados persistidos no volume nomeado `postgres-data` — sobrevive a `docker compose down`, só some com `docker compose down -v`.

Quem aplica o schema é o [`DbSeeder`](EventFlow/Infrastructure/Data/DbSeeder.cs), chamado no `Program.cs` ao subir:

1. `MigrateAsync()` aplica as migrations pendentes
2. Se não houver nenhum usuário, insere os dados de exemplo
3. Se já houver, não faz nada — seu banco de trabalho nunca é sobrescrito

> **Identificadores em Postgres são case-sensitive quando usados sem aspas.** O EF Core cria tabelas/colunas em PascalCase (igual ao nome da classe/propriedade C#). Uma query manual direto no banco precisa de aspas duplas: `SELECT * FROM "Eventos" WHERE "Titulo" = '...'` — sem aspas, o Postgres procura `eventos` (minúsculo) e não acha nada.

### Zerar e recomeçar

```bash
docker compose down -v
docker compose up -d --build
```

### Ver os dados

Abra o **DBeaver**: *Nova Conexão → PostgreSQL* com:
```
Host: localhost
Port: 5432
Database: eventflow
Username: eventflow
Password: eventflow
```

---

## Estrutura

```
EventFlow/
├─ Domain/                 # o coração — não depende de ninguém
│  ├─ Entity/              # Usuario, Evento, Participante, Ingresso, Sessao
│  ├─ Enums/               # PerfilUsuario, StatusIngresso
│  ├─ Options/             # JwtOptions, GoogleAuthOptions, EmailOptions
│  └─ Interface/           # contratos dos serviços (IEventoService, IEmailService...)
├─ Application/            # as regras de negócio
│  ├─ DTOs/Request/        # o que entra pela API
│  ├─ DTOs/Response/       # o que sai (nunca a entidade crua)
│  └─ Services/            # implementações (EventoService, IngressoService...)
├─ Infrastructure/
│  ├─ Data/                # EventFlowDbContext, DbSeeder
│  └─ Email/                # EmailService (MailKit/Gmail SMTP)
├─ Api/Controllers/        # a porta de entrada
└─ Migrations/             # histórico do schema
```

As dependências apontam para dentro: `Api` → `Application` → `Domain`, com `Infrastructure` implementando contratos definidos no `Domain` (ex.: `IEmailService` no `Domain`, `EmailService` na `Infrastructure`).

---

## Como a autenticação funciona

Dois tokens, com papéis diferentes:

| | Cookie `acesso` | Cookie `sessao` |
|---|---|---|
| O que é | JWT assinado | string aleatória de 32 bytes |
| Dura | 15 minutos | 7 dias |
| Vai em | toda requisição | só em `/api/auth` |
| Guardado no banco | não | sim, como hash |

O cookie `acesso` é verificado só pela assinatura — o banco nem é consultado. Quando ele expira, o cliente chama `/api/auth/renovar`, que confere a linha na tabela `Sessoes` e emite um par novo.

É essa tabela que torna a **revogação** possível: o `logout` marca a linha como revogada, e a sessão acaba de verdade. Só com JWT puro isso seria impossível — não haveria onde registrar que um token deixou de valer.

Cada renovação queima o token de sessão usado (**rotação**). Se um token já encerrado reaparecer, o sistema entende que existe uma cópia em circulação e **derruba todas as sessões** daquele usuário.

> Nos padrões (OAuth 2.0) e na maioria dos tutoriais, o que aqui se chama **sessão** é chamado de *refresh token*. O nome foi trocado por clareza: `acesso` e `sessao` não se confundem entre si como *access token* e *refresh token*.

---

## Solução de problemas

| Sintoma | Causa | Correção |
|---|---|---|
| `InvalidOperationException: Jwt:Key não configurada` | pulou o passo 2.1 | configure `.env` (Docker) ou User Secrets (local) |
| App não sobe, reclama de `Authentication:Google:ClientSecret` | pulou o passo 2.2 | configure `ClientId`/`ClientSecret` |
| App fica reiniciando sem parar em `docker compose up` (loop infinito) | `Email:From`/`Email:AppPassword` vazios — `ValidateOnStart` derruba o app no boot, e `restart: on-failure` reinicia sem parar | preencha `EMAIL_FROM`/`EMAIL_APP_PASSWORD` no `.env` e rode `docker compose up -d --build` de novo |
| `AuthenticationFailureException: The oauth state was missing or invalid` | redirect URI cadastrada no Google diferente do `CallbackPath`, ou app reiniciada no meio do login | confira se a URI no Google Console bate exatamente com `/api/auth/google/signin-callback`; refaça o login sem reiniciar o servidor no meio |
| Login dá 200 mas tudo depois dá 401 (modo local) | rodando em `http` | use o perfil **https** — `Secure = true` faz o navegador descartar o cookie em HTTP |
| Query direto no Postgres não acha a tabela | identificador sem aspas duplas | `SELECT * FROM "Eventos"`, não `SELECT * FROM Eventos` — Postgres dobra pra minúsculo sem aspas |
| Container do Postgres não sobe / porta 5432 ocupada | outro Postgres (nativo ou outro container) já usando a porta | pare o serviço nativo, ou troque o mapeamento de porta no `docker-compose.yml` |
| `401` ao abrir `/hangfire` via Docker | filtro padrão do Hangfire só aceita requisição "local"; o NAT do Docker quebra essa detecção | já resolvido no projeto via Basic Auth (`Hangfire.Dashboard.Basic.Authentication`) — confira usuário/senha no `Program.cs` |
| "Sua conexão não é particular" no navegador (modo local) | certificado de dev | `dotnet dev-certs https --trust` |
| `dotnet ef` não é um comando | ferramenta não instalada | `dotnet tool install --global dotnet-ef` |

---

## Stack

- **ASP.NET Core 10** — Web API com controllers
- **Entity Framework Core 10** + **PostgreSQL** (Npgsql), rodando via Docker
- **Hangfire** — jobs em background (recurring + fire-and-forget), storage no Postgres
- **MailKit** — envio de e-mail real via SMTP (Gmail)
- **JWT Bearer** com token em cookie `HttpOnly` / `Secure` / `SameSite=Strict`
- **BCrypt.Net-Next** — hash de senha, custo 11
- **Swashbuckle** — Swagger UI (apenas em Development)
- **Docker Compose** — orquestra API + Postgres
