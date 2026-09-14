# Cache em C# / ASP.NET Core — Resumo de Estudo

## Conceito geral

Cache = guardar um resultado já calculado/buscado, pra não repetir o trabalho (ex: não bater no banco de novo). Usado principalmente em **leituras (queries)**, nunca substitui uma escrita.

Fluxo padrão de qualquer cache:
1. Chegou uma leitura? Tenta achar no cache primeiro.
2. Achou → retorna direto, sem tocar no banco.
3. Não achou → busca no banco, salva no cache, retorna.
4. Alguém deu update/delete naquele dado → **remove/invalida o cache** (senão fica servindo dado velho).

O passo 4 é o mais esquecido e a causa mais comum de bug de "cache desatualizado".

---

## Memory Cache

- Vive na **RAM do próprio processo** da API.
- Nativo do .NET (`Microsoft.Extensions.Caching.Memory`), não precisa instalar NuGet.
- **Limitações**: some quando a API reinicia; não é compartilhado entre múltiplas instâncias da API (cada uma tem seu próprio cache isolado).

**Setup (`Program.cs`):**
```csharp
builder.Services.AddMemoryCache();
```

**Uso (`IMemoryCache`, injetado via DI no construtor):**
```csharp
public async Task<Evento?> ObterPorIdAsync(int id)
{
    if (_cache.TryGetValue($"Evento_{id}", out Evento? cacheado))
    {
        return cacheado;
    }

    var evento = await _repository.ObterPorIdAsync(id);

    if (evento is not null)
    {
        _cache.Set($"Evento_{id}", evento, TimeSpan.FromMinutes(10));
    }

    return evento;
}
```

**Invalidação (depois de qualquer escrita bem-sucedida):**
```csharp
await _context.SaveChangesAsync();
_cache.Remove($"Evento_{id}");
```

### `TryGetValue` e o `out`

`TryGetValue(key, out value)` devolve duas coisas ao mesmo tempo: um `bool` (achou ou não) e o valor (via `out`). Padrão comum em várias APIs do .NET (`TryParse`, `TryAdd`, etc.) — evita a ambiguidade de "não achou" vs. "achou só que é null".

O tipo do `out` precisa ser nullable (`Evento?`, `IEnumerable<Evento>?`), senão o compilador reclama (warning `CS8600`/`CS8601`). Dentro do `if`, é seguro usar `!` (null-forgiving operator) pra dizer "aqui não é null, confio":
```csharp
return cacheado!;
```

### Onde colocar a lógica de cache

Não no Repository (ele deve ter responsabilidade única: falar com o banco). Colocar no **Service** (mais simples, bom pra começar) ou, avançando, num **Decorator** que embrulha o Repository por fora (mais "correto" arquiteturalmente, mas mais complexo).

### O que NÃO cachear

- Dados sensíveis à concorrência (ex: contagem de vagas/limites, verificação de duplicidade antes de criar algo) — cachear pode permitir passar de um limite ou duplicar um registro único.
- Dados de segurança (senha/hash), a menos que muito bem pensado.
- Qualquer leitura usada como validação em tempo real dentro de um fluxo de escrita.

### Onde NÃO entra no projeto

`IMemoryCache` **não é middleware** — não tem nenhum `app.Use...` associado. Ele é só um objeto consultado manualmente dentro do código de negócio (`_cache.TryGetValue(...)`), diferente do pipeline HTTP (`app.UseAuthentication()`, `app.UseAuthorization()`, etc.), que intercepta toda requisição em ordem.

Isso é diferente do **Response Caching** (`app.UseResponseCaching()`), que é um middleware de verdade e cacheia a resposta HTTP inteira, baseado em headers — ferramenta bem diferente do `IMemoryCache`.

---

## Distributed Cache (Redis)

### Por que existe

Resolve a limitação do Memory Cache: quando a API roda em **múltiplas instâncias** (escalonamento horizontal), cada instância tem seu Memory Cache isolado — uma instância pode invalidar seu cache e a outra continuar servindo dado velho. O Distributed Cache fica **fora do processo**, num servidor externo (Redis), compartilhado por todas as instâncias.

### Diferenças principais em relação ao Memory Cache

| | Memory Cache | Distributed Cache |
|---|---|---|
| Onde vive | Dentro do processo da API | Servidor externo (Redis) |
| Interface | `IMemoryCache` | `IDistributedCache` |
| O que guarda | Objeto .NET direto | `byte[]` (precisa serializar) |
| Leitura | `TryGetValue` (síncrono) | `GetAsync` (assíncrono, devolve `byte[]?`) |
| Sobrevive a restart da API? | Não | Sim |
| Compartilhado entre instâncias? | Não | Sim |
| Precisa instalar pacote? | Não (nativo) | Sim (`Microsoft.Extensions.Caching.StackExchangeRedis`) |

### Setup

```
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

Redis local via Docker:
```
docker run -d --name redis-eventflow -p 6379:6379 redis
```

`Program.cs`:
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "EventFlow_";
});
```

### Uso (precisa serializar/desserializar manualmente)

```csharp
public async Task<Evento?> ObterPorIdAsync(int id)
{
    string chave = $"Evento_{id}";
    byte[]? bytesCache = await _cache.GetAsync(chave);

    if (bytesCache is not null)
    {
        string json = Encoding.UTF8.GetString(bytesCache);
        return JsonSerializer.Deserialize<Evento>(json);
    }

    var evento = await _eventos.ObterPorIdAsync(id);

    if (evento is not null)
    {
        string jsonParaSalvar = JsonSerializer.Serialize(evento);
        byte[] bytesParaSalvar = Encoding.UTF8.GetBytes(jsonParaSalvar);

        var opcoes = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await _cache.SetAsync(chave, bytesParaSalvar, opcoes);
    }

    return evento;
}
```

Invalidação:
```csharp
await _cache.RemoveAsync($"Evento_{id}");
```

---

## Erros comuns que já caí e como resolvi

- **Construtor marcado `static` sem querer** → gera cascata de erros (`CS0515`, `CS0132`, `CS0120`, `CS8618`, `CS8603`). Corrigir tirando o `static` do construtor.
- **`out` sem `?`** → warning de nulidade. Corrigir declarando o tipo como nullable (`Evento?`, `IEnumerable<T>?`).
- **Esquecer `async` no método que usa `await`** → não compila.
- **Chamar método com nome errado do repository** (ex: `ListarEventosAsync()` quando a interface tem `ListarAsync()`) → não compila, olhar a interface real antes de escrever a chamada.
- **Esquecer o `_cache.Remove`/`RemoveAsync` depois de uma escrita** → não é erro de compilação, é bug silencioso de dado desatualizado.

---

## useMemo (React) vs Memory Cache — onde a analogia quebra

Parecido: os dois evitam recalcular/rebuscar algo já obtido.

Diferente: `useMemo` invalida por **dependência** (array de deps, síncrono, por componente) e morre com o unmount do componente. Memory Cache invalida por **tempo** (TTL) ou remoção manual, não sabe se o dado mudou sozinho, e é compartilhado entre **todas** as requisições/usuários no mesmo processo do servidor — não é por sessão nem por render.
