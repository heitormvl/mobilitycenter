using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobilityCenter.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddBanheiroEHorariosFuncionamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TemBanheiro",
                table: "Bicicletarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "HorariosFuncionamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BicicletarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraAbertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFechamento = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosFuncionamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosFuncionamento_Bicicletarios_BicicletarioId",
                        column: x => x.BicicletarioId,
                        principalTable: "Bicicletarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosFuncionamento_BicicletarioId_DiaSemana",
                table: "HorariosFuncionamento",
                columns: new[] { "BicicletarioId", "DiaSemana" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosFuncionamento");

            migrationBuilder.DropColumn(
                name: "TemBanheiro",
                table: "Bicicletarios");
        }
    }
}
