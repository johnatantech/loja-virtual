using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loja.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class FotosDosProdutos : Migration
    {
        /// <inheritdoc />
        // Migration só de dados: coloca as fotos nos produtos de demonstração já cadastrados.
        // Só preenche quem está sem imagem, para não sobrescrever uma foto trocada pelo painel.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DefinirFoto(migrationBuilder, "fone-de-ouvido-bluetooth");
            DefinirFoto(migrationBuilder, "teclado-mecanico");
            DefinirFoto(migrationBuilder, "mouse-sem-fio");
            DefinirFoto(migrationBuilder, "monitor-27-polegadas");
            DefinirFoto(migrationBuilder, "codigo-limpo");
            DefinirFoto(migrationBuilder, "arquitetura-limpa");
            DefinirFoto(migrationBuilder, "python-para-analise-de-dados");
            DefinirFoto(migrationBuilder, "cafeteira-italiana");
            DefinirFoto(migrationBuilder, "jogo-de-panelas-antiaderente");
            DefinirFoto(migrationBuilder, "garrafa-termica-1-litro");
            DefinirFoto(migrationBuilder, "tapete-de-yoga");
            DefinirFoto(migrationBuilder, "bicicleta-aro-29");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Produtos\" SET \"ImagemUrl\" = NULL WHERE \"ImagemUrl\" = '/produtos/' || \"Slug\" || '.webp';");
        }

        private static void DefinirFoto(MigrationBuilder migrationBuilder, string slug) =>
            migrationBuilder.Sql(
                $"UPDATE \"Produtos\" SET \"ImagemUrl\" = '/produtos/{slug}.webp' WHERE \"Slug\" = '{slug}' AND \"ImagemUrl\" IS NULL;");
    }
}
