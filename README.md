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
| API | ASP.NET Core (.NET 10 LTS), Minimal APIs, OpenAPI + Scalar, Identity + JWT |
| Dados | SQL Server 2022, Entity Framework Core 10 |
| Testes | xUnit, Testcontainers (SQL Server real), Vitest + Angular Testing |
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
| 2 | Catálogo: busca, filtros, paginação e página de produto | ✅ |
| 3 | Contas e carrinho: autenticação JWT e carrinho persistido | ✅ |
| 4 | Checkout: pedido, Mercado Pago (sandbox), webhook idempotente, estoque | ✅ |
| 5 | Painel administrativo: produtos, estoque, pedidos e dashboard | ✅ |
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

### Pagamento com Mercado Pago (opcional)
Por padrão a loja usa o **pagamento simulado**. Para usar o sandbox do Mercado Pago:

1. Em [Suas integrações](https://www.mercadopago.com.br/developers/panel/app), crie uma aplicação
   do tipo Checkout Pro e copie o **Access Token de teste**.
2. Guarde as configurações nos user-secrets da API (nunca no código):
   ```bash
   cd src/Loja.Api
   dotnet user-secrets set "Pagamentos:Gateway" "MercadoPago"
   dotnet user-secrets set "MercadoPago:AccessToken" "TEST-..."
   dotnet user-secrets set "MercadoPago:UrlLoja" "<endereço público da porta 4200>"
   ```
3. Para receber o webhook, a API precisa de um endereço público. No Codespaces, torne a porta
   5120 pública (aba **Portas**, botão direito, **Visibilidade da porta**, **Pública**), defina
   `MercadoPago:UrlPublicaApi` com esse endereço e, no painel da aplicação, em **Webhooks**,
   cadastre `<endereço>/api/pagamentos/webhook/mercadopago` com o evento **Pagamentos**.
   Copie a **assinatura secreta** gerada para `MercadoPago:SegredoWebhook`.
4. Pague com uma conta compradora de teste e os cartões de teste do Mercado Pago.
5. Ao terminar, volte a porta 5120 para **Privada**.

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
- **Filtros na URL**: busca, categoria, preço, ordenação e página ficam na query string, então
  dá para recarregar, usar o botão voltar e compartilhar um link já filtrado.
- **Validação dos filtros na API**: página, tamanho, faixa de preço e ordenação inválidos
  retornam 400 com os erros por campo, em vez de consultas imprevisíveis no banco.
- **Ordenação estável na paginação**: o desempate pelo Id garante que um produto nunca apareça
  em duas páginas nem suma entre elas.
- **Estados de tela explícitos** no Angular (carregando, pronto, não encontrado, erro) em um
  único tipo, em vez de várias flags que podem se contradizer.
- **Autenticação com ASP.NET Core Identity e JWT**: senhas com hash do Identity, política de
  senha forte, bloqueio da conta por 15 minutos após 5 tentativas erradas e limite de
  requisições por IP no login e no cadastro (proteção contra força bruta).
- **Login não revela quais e-mails têm conta**: e-mail inexistente e senha errada recebem a
  mesma resposta.
- **Chave do JWT fora do código**: gerada aleatoriamente pelo `setup-dev.sh` nos user-secrets;
  a API nem inicia sem uma chave de pelo menos 32 bytes.
- **Carrinho de visitante + carrinho da conta**: sem login, os itens ficam no navegador e são
  precificados pela API; ao entrar, são mesclados ao carrinho salvo na conta (somando
  quantidades e respeitando estoque e limite por item).
- **Preço sempre atual no carrinho**: o carrinho guarda só produto e quantidade; nome, preço e
  estoque vêm do catálogo a cada leitura, com avisos quando algo mudou (esgotou, poucas unidades).
- **Redirecionamento seguro após o login**: só caminhos internos são aceitos, evitando que um
  link malicioso leve o usuário para outro site (*open redirect*).
- **Gateway de pagamento atrás de uma interface** (`IGatewayPagamento`): o simulado é o padrão
  (desenvolvimento, testes e demo funcionam sem conta externa) e o Mercado Pago é ativado por
  configuração. Trocar de gateway não muda nada no fluxo do pedido.
- **Dados de cartão nunca passam pela loja**: o cliente paga na página do Mercado Pago
  (Checkout Pro), o que dispensa as exigências do PCI DSS.
- **Webhook seguro e idempotente**: a assinatura HMAC-SHA256 (`x-signature`) é conferida em tempo
  constante; o pagamento é sempre consultado na API do gateway (nunca se confia no corpo da
  notificação); o valor pago precisa bater com o total; e a mesma notificação repetida não
  confirma o pedido duas vezes (índice único no id do pagamento).
- **Estoque sem venda dupla**: pedido, baixa de estoque e carrinho vazio são gravados na mesma
  transação; se duas compras disputam a última unidade, o `rowversion` detecta o conflito e a
  operação é refeita com o estoque atualizado. Um teste dispara compras simultâneas para provar.
- **Frete pela faixa do CEP, não pela UF digitada**: o cliente não consegue pagar menos frete
  informando outro estado.
- **Pedido guarda preços e endereço do momento da compra**, imunes a mudanças posteriores.
- **Datas sempre em UTC** no banco e na API; o navegador mostra no fuso de quem acessa.
- **Painel protegido no servidor, não só na tela**: todas as rotas `/api/admin` exigem o perfil
  Admin (cliente logado recebe 403); o guard do Angular só melhora a navegação.
- **Administrador sem senha no código**: criado na inicialização com e-mail e senha aleatória
  gerados pelo `setup-dev.sh` nos user-secrets.
- **Produto desativado sai da vitrine sem apagar histórico**: pedidos antigos continuam íntegros.
- **Pedido avança pela máquina de estados do domínio**: o painel não consegue, por exemplo,
  enviar um pedido não pago ou cancelar um já entregue.
- **Erros de negócio registrados como aviso**: estoque insuficiente ou ação inválida viram 400
  (e conflitos de concorrência, 409) sem stack trace no log; só falhas reais aparecem como erro.
- **Um SQL Server para toda a suíte de integração** (coleção do xUnit), em vez de um contêiner
  por classe de teste.

## Endpoints atuais
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/categorias` | Categorias com a quantidade de produtos ativos |
| GET | `/api/produtos` | Produtos com `busca`, `categoria`, `precoMin`, `precoMax`, `ordenacao` (`nome`, `menor-preco`, `maior-preco`), `pagina` e `tamanhoPagina` |
| GET | `/api/produtos/{slug}` | Detalhes de um produto (404 se não existir) |
| POST | `/api/contas/cadastro` | Cria conta de cliente e devolve a sessão (token JWT) |
| POST | `/api/contas/login` | Autentica e devolve a sessão |
| GET | `/api/contas/eu` | Dados do usuário autenticado 🔒 |
| POST | `/api/carrinho/cotacao` | Precifica itens do carrinho de visitante, sem salvar |
| GET | `/api/carrinho` | Carrinho da conta 🔒 |
| PUT | `/api/carrinho/itens/{produtoId}` | Define a quantidade de um produto (0 remove) 🔒 |
| DELETE | `/api/carrinho/itens/{produtoId}` | Remove um produto 🔒 |
| POST | `/api/carrinho/mesclar` | Junta o carrinho de visitante ao da conta 🔒 |
| DELETE | `/api/carrinho` | Esvazia o carrinho 🔒 |
| GET | `/api/checkout/frete?cep=` | Frete do carrinho atual para um CEP 🔒 |
| POST | `/api/pedidos` | Fecha o pedido a partir do carrinho e devolve o link de pagamento 🔒 |
| GET | `/api/pedidos` | Pedidos do cliente 🔒 |
| GET | `/api/pedidos/{id}` | Detalhes de um pedido (404 para pedidos de outras pessoas) 🔒 |
| POST | `/api/pedidos/{id}/cancelar` | Cancela um pedido não pago e devolve o estoque 🔒 |
| POST | `/api/pedidos/{id}/pagamento` | Gera de novo o link de pagamento 🔒 |
| POST | `/api/pagamentos/webhook/mercadopago` | Notificações do Mercado Pago (exige assinatura válida) |
| POST | `/api/pagamentos/simulado/{id}` | Aprova ou recusa um pagamento no gateway simulado 🔒 |
| GET | `/api/admin/dashboard` | Faturamento, pedidos por status, estoque baixo, mais vendidos 👑 |
| GET/POST/PUT | `/api/admin/produtos` | Listar (com inativos), criar e editar produtos 👑 |
| POST | `/api/admin/produtos/{id}/estoque` | Repor estoque 👑 |
| POST | `/api/admin/produtos/{id}/ativar` · `/desativar` | Mostrar ou tirar da vitrine 👑 |
| GET | `/api/admin/pedidos?status=` | Pedidos de todos os clientes 👑 |
| POST | `/api/admin/pedidos/{id}/enviar` · `/entregar` · `/cancelar` | Avançar o pedido 👑 |
| GET | `/api/admin/clientes` | Clientes, bloqueios e quantidade de pedidos 👑 |
| POST | `/api/admin/clientes/{id}/desbloquear` | Liberar conta bloqueada por tentativas 👑 |
| GET | `/health` | Saúde da API e conexão com o banco |
| GET | `/scalar` | Documentação interativa (só em desenvolvimento) |

🔒 = exige o cabeçalho `Authorization: Bearer <token>`. 👑 = exige também o perfil Admin.

O acesso de administrador é criado pelo `setup-dev.sh`: e-mail `admin@loja.local` e senha
aleatória, que fica no arquivo `.env` (fora do Git).
