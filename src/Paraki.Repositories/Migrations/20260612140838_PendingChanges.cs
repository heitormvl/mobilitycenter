using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paraki.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SugestoesEdicao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BicicletarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    AutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DadosEdicao = table.Column<string>(type: "text", nullable: false),
                    MotivoRejeicao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvaliadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SugestoesEdicao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SugestoesEdicao_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SugestoesEdicao_AspNetUsers_RevisorId",
                        column: x => x.RevisorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SugestoesEdicao_Bicicletarios_BicicletarioId",
                        column: x => x.BicicletarioId,
                        principalTable: "Bicicletarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesEdicao_AutorId",
                table: "SugestoesEdicao",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesEdicao_BicicletarioId",
                table: "SugestoesEdicao",
                column: "BicicletarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesEdicao_RevisorId",
                table: "SugestoesEdicao",
                column: "RevisorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SugestoesEdicao");
        }
    }
}
