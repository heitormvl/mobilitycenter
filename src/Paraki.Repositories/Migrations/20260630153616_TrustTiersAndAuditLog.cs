using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paraki.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class TrustTiersAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AplicadaAutomaticamente",
                table: "SugestoesEdicao",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DadosAnteriores",
                table: "SugestoesEdicao",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CriadorId",
                table: "Bicicletarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusAprovacao",
                table: "Bicicletarios",
                type: "integer",
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.AddColumn<int>(
                name: "PontosAprovados",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TierOverride",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SnapshotsBicicletario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    TemTomada = table.Column<bool>(type: "boolean", nullable: false),
                    TemCalibrador = table.Column<bool>(type: "boolean", nullable: false),
                    TemVestiario = table.Column<bool>(type: "boolean", nullable: false),
                    TemArmario = table.Column<bool>(type: "boolean", nullable: false),
                    TemEspacoManutencao = table.Column<bool>(type: "boolean", nullable: false),
                    TemCadeado = table.Column<bool>(type: "boolean", nullable: false),
                    TemBanheiro = table.Column<bool>(type: "boolean", nullable: false),
                    AcessoLivre = table.Column<bool>(type: "boolean", nullable: false),
                    AcessoPago = table.Column<bool>(type: "boolean", nullable: false),
                    AcessoCadastro = table.Column<bool>(type: "boolean", nullable: false),
                    AcessoMensal = table.Column<bool>(type: "boolean", nullable: false),
                    VeiculosSuportados = table.Column<int>(type: "integer", nullable: false),
                    StatusAprovacao = table.Column<int>(type: "integer", nullable: false),
                    Deletado = table.Column<bool>(type: "boolean", nullable: false),
                    HorariosJson = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotsBicicletario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoAcao = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeUsuario = table.Column<string>(type: "text", nullable: false),
                    BicicletarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    SugestaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotAntesId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotDepoisId = table.Column<Guid>(type: "uuid", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_Bicicletarios_BicicletarioId",
                        column: x => x.BicicletarioId,
                        principalTable: "Bicicletarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_SnapshotsBicicletario_SnapshotAntesId",
                        column: x => x.SnapshotAntesId,
                        principalTable: "SnapshotsBicicletario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LogsAuditoria_SnapshotsBicicletario_SnapshotDepoisId",
                        column: x => x.SnapshotDepoisId,
                        principalTable: "SnapshotsBicicletario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bicicletarios_CriadorId",
                table: "Bicicletarios",
                column: "CriadorId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_BicicletarioId",
                table: "LogsAuditoria",
                column: "BicicletarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_SnapshotAntesId",
                table: "LogsAuditoria",
                column: "SnapshotAntesId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_SnapshotDepoisId",
                table: "LogsAuditoria",
                column: "SnapshotDepoisId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_UsuarioId",
                table: "LogsAuditoria",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bicicletarios_AspNetUsers_CriadorId",
                table: "Bicicletarios",
                column: "CriadorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bicicletarios_AspNetUsers_CriadorId",
                table: "Bicicletarios");

            migrationBuilder.DropTable(
                name: "LogsAuditoria");

            migrationBuilder.DropTable(
                name: "SnapshotsBicicletario");

            migrationBuilder.DropIndex(
                name: "IX_Bicicletarios_CriadorId",
                table: "Bicicletarios");

            migrationBuilder.DropColumn(
                name: "AplicadaAutomaticamente",
                table: "SugestoesEdicao");

            migrationBuilder.DropColumn(
                name: "DadosAnteriores",
                table: "SugestoesEdicao");

            migrationBuilder.DropColumn(
                name: "CriadorId",
                table: "Bicicletarios");

            migrationBuilder.DropColumn(
                name: "StatusAprovacao",
                table: "Bicicletarios");

            migrationBuilder.DropColumn(
                name: "PontosAprovados",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TierOverride",
                table: "AspNetUsers");
        }
    }
}
