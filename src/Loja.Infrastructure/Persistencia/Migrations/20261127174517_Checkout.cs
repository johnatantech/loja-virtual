using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loja.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Checkout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntregaBairro",
                table: "Pedidos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaCep",
                table: "Pedidos",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaCidade",
                table: "Pedidos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaComplemento",
                table: "Pedidos",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaDestinatario",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaLogradouro",
                table: "Pedidos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaNumero",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntregaUf",
                table: "Pedidos",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkPagamento",
                table: "Pedidos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PagamentoExternoId",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PagoEm",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrazoEntregaDias",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFrete",
                table: "Pedidos",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_PagamentoExternoId",
                table: "Pedidos",
                column: "PagamentoExternoId",
                unique: true,
                filter: "[PagamentoExternoId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Pedidos_ValorFrete",
                table: "Pedidos",
                sql: "[ValorFrete] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pedidos_PagamentoExternoId",
                table: "Pedidos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Pedidos_ValorFrete",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaBairro",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaCep",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaCidade",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaComplemento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaDestinatario",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaLogradouro",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaNumero",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EntregaUf",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "LinkPagamento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PagamentoExternoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PagoEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PrazoEntregaDias",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ValorFrete",
                table: "Pedidos");
        }
    }
}
