# 🛒 Loja Virtual: e-commerce full stack com .NET 10 e Angular

E-commerce completo de demonstração: catálogo, carrinho, checkout com pagamento em sandbox,
área do cliente e painel administrativo. Projeto de portfólio com foco em **regras de negócio
bem testadas, segurança e arquitetura organizada**.

> ⚠️ Loja fictícia: os pagamentos usam apenas o ambiente de testes do gateway e nenhum dado de
> cartão é armazenado pela aplicação.

## Stack
| Camada | Tecnologia |
|--------|------------|
| Front-end | Angular 22 (standalone components, signals) |
| API | ASP.NET Core (.NET 10 LTS), Minimal APIs, OpenAPI + Scalar |
| Dados | SQL Server 2022, Entity Framework Core 10 |
| Testes | xUnit, Testcontainers (SQL Server real), Vitest |
| Ambiente | GitHub Codespaces / Dev Containers, Docker Compose |

## Arquitetura
```
web/ (Angular)  →  Loja.Api  →  Loja.Application  →  Loja.Domain
                        ↘  Loja.Infrastructure (EF Core, SQL Server) ↗
```
- **Domain**: entidades e regras de negócio, sem dependência de banco ou framework.
- **Application**: casos de uso e contratos (interfaces) que a Infrastructure implementa.
- **Infrastructure**: EF Core, migrations, consultas e, nas próximas fases, o gateway de pagamento.
- **Api**: endpoints, tratamento de erros (ProblemDetails), health check e documentação.

## Roadmap
| Fase | Entrega | Status |
|------|---------|--------|
| 1 | Fundação: solução, domínio, banco, Codespaces, integração front ↔ API | ✅ |
| 2 | Catálogo: busca, filtros, paginação e página de produto | ⏳ |
| 3 | Contas e carrinho: autenticação JWT e carrinho persistido | ⏳ |
| 4 | Checkout: pedido, Mercado Pago (sandbox), webhook idempotente, estoque | ⏳ |
| 5 | Painel administrativo: produtos, estoque, pedidos e dashboard | ⏳ |
| 6 | Qualidade e deploy: testes E2E (Playwright), CI/CD e publicação | ⏳ |

## Como rodar

### No GitHub Codespaces (recomendado)
1. No repositório, clique em **Code → Codespaces → Create codespace on main**.
2. Aguarde a configuração automática (`scripts/setup-dev.sh`): ela gera a senha do banco,
   sobe o SQL Server, grava a connection string nos user-secrets e instala as dependências.
3. Em dois terminais:
   ```bash
   dotnet run --project src/Loja.Api   # API: http://localhost:5120 (docs em /scalar)
   cd web && npm start                 # Loja: http://localhost:4200
   ```

### Localmente
Requer .NET 10 SDK, Node.js LTS e Docker. Rode `bash scripts/setup-dev.sh` e siga o passo 3.

### Testes
```bash
dotnet test                 # domínio + integração (sobe um SQL Server via Testcontainers)
cd web && npm test          # front-end
```

## Decisões técnicas
- **Regras de negócio no domínio**: produto não vende além do estoque, pedido segue uma máquina
  de estados (aguardando pagamento → pago → enviado → entregue) e itens guardam o preço do
  momento da compra.
- **Concorrência otimista no estoque** (`rowversion`): duas compras simultâneas da última
  unidade não resultam em estoque negativo; a segunda falha ao salvar.
- **Regras repetidas no banco** (check constraints de preço e estoque) como última linha de defesa.
- **Nenhum segredo no código**: senha do banco gerada aleatoriamente no `.env` (fora do Git) e
  connection string em user-secrets; em produção, variáveis de ambiente ou Azure Key Vault.
- **Testes de integração contra SQL Server real** (Testcontainers), não um banco em memória que
  se comporta diferente do de produção.
- **Erros de negócio viram HTTP 400** com ProblemDetails, em vez de erro 500 genérico.

## Endpoints atuais
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/categorias` | Categorias com a quantidade de produtos ativos |
| GET | `/health` | Saúde da API e conexão com o banco |
| GET | `/scalar` | Documentação interativa (só em desenvolvimento) |
