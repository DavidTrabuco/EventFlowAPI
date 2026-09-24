# --- Etapa 1: build/publish -------------------------------------------------
# SDK completo (compilador, NuGet etc) — so existe nesta etapa, nao vai pra
# imagem final.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia so o .csproj primeiro. Assim, enquanto voce so mexe no codigo (.cs),
# o Docker reaproveita a camada do "dotnet restore" em vez de baixar os
# pacotes do NuGet de novo a cada build.
COPY EventFlow/EventFlow.csproj EventFlow/
RUN dotnet restore EventFlow/EventFlow.csproj

# Agora sim copia o resto do codigo do projeto principal.
COPY EventFlow/ EventFlow/
RUN dotnet publish EventFlow/EventFlow.csproj -c Release -o /app/publish --no-restore

# --- Etapa 2: imagem final ---------------------------------------------------
# Só o runtime do ASP.NET Core (bem menor que o SDK) — é o que roda em produção.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# A imagem oficial do .NET 10 já escuta em http://+:8080 por padrão.
EXPOSE 8080

ENTRYPOINT ["dotnet", "EventFlow.dll"]
