using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobilityCenter.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoPerfilUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilUrl",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPerfilUrl",
                table: "AspNetUsers");
        }
    }
}
