# Imagem da API para publicação (Render). Build em duas etapas: o SDK compila e só o
# runtime, bem menor, vai para a imagem final.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro só com os .csproj: enquanto as dependências não mudam, essa camada fica em cache.
COPY src/Loja.Domain/Loja.Domain.csproj src/Loja.Domain/
COPY src/Loja.Application/Loja.Application.csproj src/Loja.Application/
COPY src/Loja.Infrastructure/Loja.Infrastructure.csproj src/Loja.Infrastructure/
COPY src/Loja.Api/Loja.Api.csproj src/Loja.Api/
RUN dotnet restore src/Loja.Api/Loja.Api.csproj

COPY src/ src/
RUN dotnet publish src/Loja.Api/Loja.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
# Roda sem privilégios de root (usuário "app" das imagens oficiais do .NET).
USER $APP_UID
# O Render informa a porta na variável PORT.
CMD ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Loja.Api.dll"]
