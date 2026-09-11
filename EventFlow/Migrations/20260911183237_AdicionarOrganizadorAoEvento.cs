using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventFlow.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarOrganizadorAoEvento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizadorId",
                table: "Eventos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_OrganizadorId",
                table: "Eventos",
                column: "OrganizadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventos_Usuarios_OrganizadorId",
                table: "Eventos",
                column: "OrganizadorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Eventos_Usuarios_OrganizadorId",
                table: "Eventos");

            migrationBuilder.DropIndex(
                name: "IX_Eventos_OrganizadorId",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "OrganizadorId",
                table: "Eventos");
        }
    }
}
