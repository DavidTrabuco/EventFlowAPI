# EventFlow

API de gestão de eventos e ingressos em ASP.NET Core 8, com autenticação JWT em cookie `HttpOnly`, sessão revogável e autorização por perfil.

Projeto de estudo — o passo a passo completo da construção está no [blueprint](https://claude.ai/code/artifact/bc81f614-f44b-4450-839e-233d96922e20).

---

## Requisitos

| | Versão | Como obter |
|---|---|---|
| .NET SDK | **8.0** ou superior | https://dotnet.microsoft.com/download |

Só isso. Não precisa instalar banco de dados: o projeto usa **SQLite**, que é um arquivo — sem servidor, sem porta, sem senha. O arquivo é criado sozinho na primeira execução, já com dados de exemplo.

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

### 2. Configurar a chave JWT

**Obrigatório.** A chave que assina os tokens não está no repositório — quem a tem consegue forjar um token dizendo ser qualquer usuário, com qualquer perfil. Por isso ela fica fora do Git, em cada máquina.

**Windows (PowerShell):**

```powershell
cd EventFlow
dotnet user-secrets init
$b = New-Object byte[] 32
([System.Security.Cryptography.RNGCryptoServiceProvider]::new()).GetBytes($b)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($b))
```

**Linux / macOS:**

```bash
cd EventFlow
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
```

Confirme:

```bash
dotnet user-secrets list
```

> A chave é gerada, nunca digitada. Uma frase como `"minha-chave-super-secreta"` tem entropia baixíssima — é palavra de dicionário, está no topo de qualquer lista de tentativas.
>
> Cada pessoa gera a própria chave. Elas **não** precisam ser iguais: quem assina e quem valida é a mesma aplicação, na mesma máquina.
>
> Em produção, use a variável de ambiente `Jwt__Key` (dois underscores no lugar dos dois pontos) ou um cofre de segredos.

### 3. Configurar o login com Google (opcional)

**Diferente da chave do passo 2, isso não pode ser gerado localmente.** `ClientId` e `ClientSecret` são credenciais emitidas pelo Google, atreladas a um projeto cadastrado no Google Cloud — não existe comando que "gere" isso na sua máquina, é preciso ir ao site.

1. Acesse [console.cloud.google.com](https://console.cloud.google.com) e crie um projeto (ou use um existente)
2. Vá em **Google Auth Platform → Público-alvo**, escolha **Externo**, preencha nome do app e email de suporte, e adicione seu email como **usuário de teste** (o app fica em modo de teste, só quem está nessa lista consegue logar)
3. Vá em **Clientes → Criar um cliente OAuth**, tipo **Aplicativo da Web**
4. Em **URIs de redirecionamento autorizados**, adicione:
   ```
   https://localhost:7076/api/auth/google/signin-callback
   ```
   Precisa bater **exatamente** com o `CallbackPath` configurado no `Program.cs` — qualquer diferença de barra ou porta e o Google recusa o login
5. Copie o **ID do cliente** e a **Chave secreta do cliente** gerados

O `ClientId` não é segredo — vai no `appsettings.json`:
```json
"Authentication": {
  "Google": {
    "ClientId": "SEU-CLIENT-ID.apps.googleusercontent.com"
  }
}
```

O `ClientSecret` é segredo — mesma regra da chave JWT, vai pro User Secrets:
```bash
dotnet user-secrets set "Authentication:Google:ClientSecret" "SEU-CLIENT-SECRET"
```

> Sem isso configurado, a aplicação recusa subir: `GoogleAuthOptions` é validado no arranque (`ValidateOnStart`), igual ao `JwtOptions`. Se você só quer testar o login local (email/senha), pode pular este passo — mas nesse caso a aplicação ainda vai exigir esses dois valores presentes (mesmo vazios não passa na validação `[Required]`). Deixe o cadastro no Google feito antes de rodar.

### 4. Rodar

```bash
cd EventFlow
dotnet run --launch-profile https
```

Na primeira execução o banco é criado e populado automaticamente (ver [DbSeeder](EventFlow/Infrastructure/Data/DbSeeder.cs)).

Abra: **https://localhost:7076/swagger**

Pelo **Visual Studio**: selecione o perfil **https** no dropdown ao lado do ▶ e pressione F5.

### Se o navegador reclamar do certificado

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

## O banco de dados

O arquivo `EventFlow/EventFlow.db` **não vai para o repositório** — está no `.gitignore`. Cada pessoa gera o seu ao rodar a aplicação pela primeira vez.

Quem cuida disso é o [`DbSeeder`](EventFlow/Infrastructure/Data/DbSeeder.cs), chamado no `Program.cs` ao subir:

1. `MigrateAsync()` aplica as migrations pendentes e cria o `.db` se não existir
2. Se não houver nenhum usuário, insere os dados de exemplo
3. Se já houver, não faz nada — seu banco de trabalho nunca é sobrescrito

Isso também resolve o `git pull`: quando alguém adiciona uma migration, o schema se atualiza sozinho na próxima execução.

> **Por que não commitar o `.db`:** é binário, então o Git não consegue mesclar — dois commits no mesmo arquivo viram conflito insolúvel. Ele também muda a cada execução (a tabela `Sessoes` cresce a cada login), deixando o `git status` sempre sujo. E o SQLite mantém escritas pendentes num arquivo `-wal` separado: commitar só o `.db` pode levar um banco **sem os dados** para quem clonar.

### Zerar e recomeçar

Apague o arquivo e rode de novo — o seeder recria tudo:

```bash
cd EventFlow
rm EventFlow.db
dotnet run --launch-profile https
```

### Ver os dados

Abra `EventFlow/EventFlow.db` no **DBeaver**: *Nova Conexão → SQLite → Path → apontar para o arquivo*. Sem host, porta, usuário ou senha.

Feche a aplicação antes de escrever pelo DBeaver — o SQLite aceita vários leitores, mas só um escritor por vez. E lembre de desconectar no DBeaver antes de apagar o banco, senão o arquivo fica travado.

---

## Estrutura

```
EventFlow/
├─ Domain/                 # o coração — não depende de ninguém
│  ├─ Entity/              # Usuario, Evento, Participante, Ingresso, Sessao
│  ├─ Enums/               # PerfilUsuario, StatusIngresso
│  └─ Interface/           # contratos dos serviços
├─ Application/            # as regras de negócio
│  ├─ DTOs/Request/        # o que entra pela API
│  ├─ DTOs/Response/       # o que sai (nunca a entidade crua)
│  └─ Services/            # implementações
├─ Infrastructure/Data/    # EventFlowDbContext
├─ Api/Controllers/        # a porta de entrada
└─ Migrations/             # histórico do schema
```

As dependências apontam para dentro: `Api` → `Application` → `Domain`. Por isso as interfaces ficam em `Domain` e as implementações em `Application`.

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
| `InvalidOperationException: Jwt:Key não configurada` | pulou o passo 2 | configure os User Secrets |
| App não sobe, reclama de `Authentication:Google:ClientSecret` | pulou o passo 3 | configure `ClientId`/`ClientSecret` (veja passo 3) |
| `AuthenticationFailureException: The oauth state was missing or invalid` | redirect URI cadastrada no Google diferente do `CallbackPath`, ou app reiniciada no meio do login | confira se a URI no Google Console bate exatamente com `/api/auth/google/signin-callback`; refaça o login sem reiniciar o servidor no meio |
| Login dá 200 mas tudo depois dá 401 | rodando em `http` | use o perfil **https** — `Secure = true` faz o navegador descartar o cookie em HTTP |
| "Sua conexão não é particular" no navegador | certificado de dev | `dotnet dev-certs https --trust` |
| `The process cannot access the file EventFlow.exe` | execução anterior ainda viva | `taskkill /IM EventFlow.exe /F` |
| Visual Studio mostra erros que não existem | cache da Lista de Erros | Compilar → Recompilar Solução |
| `dotnet ef` não é um comando | ferramenta não instalada | `dotnet tool install --global dotnet-ef` |

---

## Stack

- **ASP.NET Core 8** — Web API com controllers
- **Entity Framework Core 8** + SQLite
- **JWT Bearer** com token em cookie `HttpOnly` / `Secure` / `SameSite=Strict`
- **BCrypt.Net-Next** — hash de senha, custo 11
- **Swashbuckle** — Swagger UI (apenas em Development)
