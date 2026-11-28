#!/usr/bin/env bash
# Prepara o ambiente de desenvolvimento: senha do banco, PostgreSQL, segredos e dependências.
# Pode ser executado mais de uma vez sem problema.
set -euo pipefail
cd "$(dirname "$0")/.."

# 1. Senha aleatória do PostgreSQL, guardada só no .env (fora do Git)
touch .env
if ! grep -q '^POSTGRES_PASSWORD=.\+' .env; then
  echo "POSTGRES_PASSWORD=$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9' | head -c 24)" >> .env
  echo "✓ Senha do banco gerada em .env"
fi
set -a; source .env; set +a

# 2. PostgreSQL em container
echo "→ Subindo o PostgreSQL..."
docker compose up -d db
for _ in $(seq 1 30); do
  [[ "$(docker inspect -f '{{.State.Health.Status}}' loja-db 2>/dev/null)" == "healthy" ]] && break
  sleep 2
done
echo "✓ PostgreSQL pronto"

# 3. Connection string nos user-secrets da API (nunca no appsettings)
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:Loja" \
  "Host=localhost;Port=5432;Database=loja;Username=loja;Password=${POSTGRES_PASSWORD}" \
  --project src/Loja.Api > /dev/null
echo "✓ Connection string salva nos user-secrets"

# 4. Chave de assinatura do JWT: gerada uma única vez, aleatória e fora do Git
if ! dotnet user-secrets list --project src/Loja.Api | grep -q '^Jwt:Chave'; then
  dotnet user-secrets set "Jwt:Chave" "$(openssl rand -base64 48)" --project src/Loja.Api > /dev/null
  echo "✓ Chave do JWT gerada nos user-secrets"
fi

# 5. Conta de administrador do painel: senha aleatória, guardada nos user-secrets e no .env
if ! dotnet user-secrets list --project src/Loja.Api | grep -q '^Admin:Senha'; then
  senha_admin="Adm$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9' | head -c 16)1"
  dotnet user-secrets set "Admin:Email" "admin@loja.local" --project src/Loja.Api > /dev/null
  dotnet user-secrets set "Admin:Senha" "${senha_admin}" --project src/Loja.Api > /dev/null
  { echo "ADMIN_EMAIL=admin@loja.local"; echo "ADMIN_SENHA=${senha_admin}"; } >> .env
  echo "✓ Administrador criado: admin@loja.local (senha no arquivo .env)"
fi

# 6. Dependências
dotnet restore
(cd web && npm ci)
echo
echo "Tudo pronto! Em dois terminais:"
echo "  dotnet run --project src/Loja.Api     # API em http://localhost:5120"
echo "  cd web && npm start                   # loja em http://localhost:4200"
