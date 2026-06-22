using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paraki.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddFotosBicicletario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosBicicletario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BicicletarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsCapa = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosBicicletario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosBicicletario_Bicicletarios_BicicletarioId",
                        column: x => x.BicicletarioId,
                        principalTable: "Bicicletarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosBicicletario_BicicletarioId",
                table: "FotosBicicletario",
                column: "BicicletarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosBicicletario");
        }
    }
}
