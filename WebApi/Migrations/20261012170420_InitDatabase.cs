using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Foglalasok_AuditLogok_AuditLogID",
                table: "Foglalasok");

            migrationBuilder.DropForeignKey(
                name: "FK_KarbantartasiIdosavok_AuditLogok_AuditLogID",
                table: "KarbantartasiIdosavok");

            migrationBuilder.DropForeignKey(
                name: "FK_MesterkulcsJogosultsagok_AuditLogok_AuditLogID",
                table: "MesterkulcsJogosultsagok");

            migrationBuilder.DropForeignKey(
                name: "FK_MesterkulcsJogosultsagok_Felhasznalok_AdminisztratorID",
                table: "MesterkulcsJogosultsagok");

            migrationBuilder.DropIndex(
                name: "IX_MesterkulcsJogosultsagok_AdminisztratorID",
                table: "MesterkulcsJogosultsagok");

            migrationBuilder.DropIndex(
                name: "IX_KulcsMozgasok_AuditLogID",
                table: "KulcsMozgasok");

            migrationBuilder.DropIndex(
                name: "IX_KarbantartasiIdosavok_AuditLogID",
                table: "KarbantartasiIdosavok");

            migrationBuilder.DropIndex(
                name: "IX_Foglalasok_AuditLogID",
                table: "Foglalasok");

            migrationBuilder.DropColumn(
                name: "FelszereltsegLista",
                table: "Termek");

            migrationBuilder.DropColumn(
                name: "AdminisztratorID",
                table: "MesterkulcsJogosultsagok");

            migrationBuilder.DropColumn(
                name: "AuditLogID",
                table: "KarbantartasiIdosavok");

            migrationBuilder.DropColumn(
                name: "AuditLogID",
                table: "Foglalasok");

            migrationBuilder.RenameColumn(
                name: "AuditLogID",
                table: "MesterkulcsJogosultsagok",
                newName: "FelhasznaloID");

            migrationBuilder.RenameIndex(
                name: "IX_MesterkulcsJogosultsagok_AuditLogID",
                table: "MesterkulcsJogosultsagok",
                newName: "IX_MesterkulcsJogosultsagok_FelhasznaloID");

            migrationBuilder.AlterColumn<int>(
                name: "TeremID",
                table: "Kulcsok",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Felhasznalok",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "Felhasznalok",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FoglalasID",
                table: "AuditLogok",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KarbantartasID",
                table: "AuditLogok",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MesterkulcsJogosultsagID",
                table: "AuditLogok",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Felszereltseg",
                columns: table => new
                {
                    FelszereltsegID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nev = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Felszereltseg", x => x.FelszereltsegID);
                });

            migrationBuilder.CreateTable(
                name: "FelszereltsegFoglalas",
                columns: table => new
                {
                    FoglalasokFoglalasID = table.Column<int>(type: "integer", nullable: false),
                    IgenyeltFelszereltsegekFelszereltsegID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelszereltsegFoglalas", x => new { x.FoglalasokFoglalasID, x.IgenyeltFelszereltsegekFelszereltsegID });
                    table.ForeignKey(
                        name: "FK_FelszereltsegFoglalas_Felszereltseg_IgenyeltFelszereltsegek~",
                        column: x => x.IgenyeltFelszereltsegekFelszereltsegID,
                        principalTable: "Felszereltseg",
                        principalColumn: "FelszereltsegID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelszereltsegFoglalas_Foglalasok_FoglalasokFoglalasID",
                        column: x => x.FoglalasokFoglalasID,
                        principalTable: "Foglalasok",
                        principalColumn: "FoglalasID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FelszereltsegTerem",
                columns: table => new
                {
                    FelszereltsegekFelszereltsegID = table.Column<int>(type: "integer", nullable: false),
                    TermekTeremID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelszereltsegTerem", x => new { x.FelszereltsegekFelszereltsegID, x.TermekTeremID });
                    table.ForeignKey(
                        name: "FK_FelszereltsegTerem_Felszereltseg_FelszereltsegekFelszerelts~",
                        column: x => x.FelszereltsegekFelszereltsegID,
                        principalTable: "Felszereltseg",
                        principalColumn: "FelszereltsegID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelszereltsegTerem_Termek_TermekTeremID",
                        column: x => x.TermekTeremID,
                        principalTable: "Termek",
                        principalColumn: "TeremID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KulcsMozgasok_AuditLogID",
                table: "KulcsMozgasok",
                column: "AuditLogID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogok_FoglalasID",
                table: "AuditLogok",
                column: "FoglalasID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogok_KarbantartasID",
                table: "AuditLogok",
                column: "KarbantartasID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogok_MesterkulcsJogosultsagID",
                table: "AuditLogok",
                column: "MesterkulcsJogosultsagID");

            migrationBuilder.CreateIndex(
                name: "IX_FelszereltsegFoglalas_IgenyeltFelszereltsegekFelszereltsegID",
                table: "FelszereltsegFoglalas",
                column: "IgenyeltFelszereltsegekFelszereltsegID");

            migrationBuilder.CreateIndex(
                name: "IX_FelszereltsegTerem_TermekTeremID",
                table: "FelszereltsegTerem",
                column: "TermekTeremID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogok_Foglalasok_FoglalasID",
                table: "AuditLogok",
                column: "FoglalasID",
                principalTable: "Foglalasok",
                principalColumn: "FoglalasID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogok_KarbantartasiIdosavok_KarbantartasID",
                table: "AuditLogok",
                column: "KarbantartasID",
                principalTable: "KarbantartasiIdosavok",
                principalColumn: "KarbantartasID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogok_MesterkulcsJogosultsagok_MesterkulcsJogosultsagID",
                table: "AuditLogok",
                column: "MesterkulcsJogosultsagID",
                principalTable: "MesterkulcsJogosultsagok",
                principalColumn: "JogosultsagID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MesterkulcsJogosultsagok_Felhasznalok_FelhasznaloID",
                table: "MesterkulcsJogosultsagok",
                column: "FelhasznaloID",
                principalTable: "Felhasznalok",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogok_Foglalasok_FoglalasID",
                table: "AuditLogok");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogok_KarbantartasiIdosavok_KarbantartasID",
                table: "AuditLogok");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogok_MesterkulcsJogosultsagok_MesterkulcsJogosultsagID",
                table: "AuditLogok");

            migrationBuilder.DropForeignKey(
                name: "FK_MesterkulcsJogosultsagok_Felhasznalok_FelhasznaloID",
                table: "MesterkulcsJogosultsagok");

            migrationBuilder.DropTable(
                name: "FelszereltsegFoglalas");

            migrationBuilder.DropTable(
                name: "FelszereltsegTerem");

            migrationBuilder.DropTable(
                name: "Felszereltseg");

            migrationBuilder.DropIndex(
                name: "IX_KulcsMozgasok_AuditLogID",
                table: "KulcsMozgasok");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogok_FoglalasID",
                table: "AuditLogok");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogok_KarbantartasID",
                table: "AuditLogok");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogok_MesterkulcsJogosultsagID",
                table: "AuditLogok");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Felhasznalok");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                table: "Felhasznalok");

            migrationBuilder.DropColumn(
                name: "FoglalasID",
                table: "AuditLogok");

            migrationBuilder.DropColumn(
                name: "KarbantartasID",
                table: "AuditLogok");

            migrationBuilder.DropColumn(
                name: "MesterkulcsJogosultsagID",
                table: "AuditLogok");

            migrationBuilder.RenameColumn(
                name: "FelhasznaloID",
                table: "MesterkulcsJogosultsagok",
                newName: "AuditLogID");

            migrationBuilder.RenameIndex(
                name: "IX_MesterkulcsJogosultsagok_FelhasznaloID",
                table: "MesterkulcsJogosultsagok",
                newName: "IX_MesterkulcsJogosultsagok_AuditLogID");

            migrationBuilder.AddColumn<int>(
                name: "FelszereltsegLista",
                table: "Termek",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AdminisztratorID",
                table: "MesterkulcsJogosultsagok",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TeremID",
                table: "Kulcsok",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuditLogID",
                table: "KarbantartasiIdosavok",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuditLogID",
                table: "Foglalasok",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MesterkulcsJogosultsagok_AdminisztratorID",
                table: "MesterkulcsJogosultsagok",
                column: "AdminisztratorID");

            migrationBuilder.CreateIndex(
                name: "IX_KulcsMozgasok_AuditLogID",
                table: "KulcsMozgasok",
                column: "AuditLogID");

            migrationBuilder.CreateIndex(
                name: "IX_KarbantartasiIdosavok_AuditLogID",
                table: "KarbantartasiIdosavok",
                column: "AuditLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Foglalasok_AuditLogID",
                table: "Foglalasok",
                column: "AuditLogID");

            migrationBuilder.AddForeignKey(
                name: "FK_Foglalasok_AuditLogok_AuditLogID",
                table: "Foglalasok",
                column: "AuditLogID",
                principalTable: "AuditLogok",
                principalColumn: "NaploID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KarbantartasiIdosavok_AuditLogok_AuditLogID",
                table: "KarbantartasiIdosavok",
                column: "AuditLogID",
                principalTable: "AuditLogok",
                principalColumn: "NaploID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MesterkulcsJogosultsagok_AuditLogok_AuditLogID",
                table: "MesterkulcsJogosultsagok",
                column: "AuditLogID",
                principalTable: "AuditLogok",
                principalColumn: "NaploID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MesterkulcsJogosultsagok_Felhasznalok_AdminisztratorID",
                table: "MesterkulcsJogosultsagok",
                column: "AdminisztratorID",
                principalTable: "Felhasznalok",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
