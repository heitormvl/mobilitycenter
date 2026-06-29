using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paraki.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddComprovanteSugestao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comprovante",
                table: "SugestoesEdicao",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComprovanteFotoKey",
                table: "SugestoesEdicao",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comprovante",
                table: "SugestoesEdicao");

            migrationBuilder.DropColumn(
                name: "ComprovanteFotoKey",
                table: "SugestoesEdicao");
        }
    }
}
