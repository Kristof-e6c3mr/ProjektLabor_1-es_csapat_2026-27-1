using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class FixRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Felhasznalok",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nev = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Jelszo = table.Column<string>(type: "text", nullable: false),
                    Szerepkor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Felhasznalok", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogok",
                columns: table => new
                {
                    NaploID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Idobelyeg = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Muvelet = table.Column<string>(type: "text", nullable: false),
                    Reszletek = table.Column<string>(type: "text", nullable: false),
                    UzemeltetesiIgazgatoID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogok", x => x.NaploID);
                    table.ForeignKey(
                        name: "FK_AuditLogok_Felhasznalok_UzemeltetesiIgazgatoID",
                        column: x => x.UzemeltetesiIgazgatoID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Termek",
                columns: table => new
                {
                    TeremID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Teremszam = table.Column<int>(type: "integer", nullable: false),
                    Ferohely = table.Column<int>(type: "integer", nullable: false),
                    Allapot = table.Column<bool>(type: "boolean", nullable: false),
                    FelszereltsegLista = table.Column<int>(type: "integer", nullable: false),
                    AdminisztratorID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Termek", x => x.TeremID);
                    table.ForeignKey(
                        name: "FK_Termek_Felhasznalok_AdminisztratorID",
                        column: x => x.AdminisztratorID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Foglalasok",
                columns: table => new
                {
                    FoglalasID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KezdoIdo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ZaroIdo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Allapot = table.Column<string>(type: "text", nullable: false),
                    OktatoID = table.Column<int>(type: "integer", nullable: false),
                    TeremID = table.Column<int>(type: "integer", nullable: false),
                    AuditLogID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foglalasok", x => x.FoglalasID);
                    table.ForeignKey(
                        name: "FK_Foglalasok_AuditLogok_AuditLogID",
                        column: x => x.AuditLogID,
                        principalTable: "AuditLogok",
                        principalColumn: "NaploID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Foglalasok_Felhasznalok_OktatoID",
                        column: x => x.OktatoID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Foglalasok_Termek_TeremID",
                        column: x => x.TeremID,
                        principalTable: "Termek",
                        principalColumn: "TeremID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Hibak",
                columns: table => new
                {
                    HibaID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Leiras = table.Column<string>(type: "text", nullable: false),
                    LetrehozasDatuma = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Allapot = table.Column<bool>(type: "boolean", nullable: false),
                    PortasID = table.Column<int>(type: "integer", nullable: false),
                    TeremID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hibak", x => x.HibaID);
                    table.ForeignKey(
                        name: "FK_Hibak_Felhasznalok_PortasID",
                        column: x => x.PortasID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Hibak_Termek_TeremID",
                        column: x => x.TeremID,
                        principalTable: "Termek",
                        principalColumn: "TeremID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KarbantartasiIdosavok",
                columns: table => new
                {
                    KarbantartasID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KezdoIdo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ZaroIdo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leiras = table.Column<string>(type: "text", nullable: false),
                    TeremID = table.Column<int>(type: "integer", nullable: false),
                    AuditLogID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KarbantartasiIdosavok", x => x.KarbantartasID);
                    table.ForeignKey(
                        name: "FK_KarbantartasiIdosavok_AuditLogok_AuditLogID",
                        column: x => x.AuditLogID,
                        principalTable: "AuditLogok",
                        principalColumn: "NaploID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KarbantartasiIdosavok_Termek_TeremID",
                        column: x => x.TeremID,
                        principalTable: "Termek",
                        principalColumn: "TeremID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kulcsok",
                columns: table => new
                {
                    KulcsID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MesterkulcsE = table.Column<bool>(type: "boolean", nullable: false),
                    KiadvaE = table.Column<bool>(type: "boolean", nullable: false),
                    TeremID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kulcsok", x => x.KulcsID);
                    table.ForeignKey(
                        name: "FK_Kulcsok_Termek_TeremID",
                        column: x => x.TeremID,
                        principalTable: "Termek",
                        principalColumn: "TeremID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KulcsMozgasok",
                columns: table => new
                {
                    MozgasID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Idobelyeg = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    KulcsID = table.Column<int>(type: "integer", nullable: false),
                    AtvevoID = table.Column<int>(type: "integer", nullable: false),
                    PortasID = table.Column<int>(type: "integer", nullable: false),
                    AuditLogID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KulcsMozgasok", x => x.MozgasID);
                    table.ForeignKey(
                        name: "FK_KulcsMozgasok_AuditLogok_AuditLogID",
                        column: x => x.AuditLogID,
                        principalTable: "AuditLogok",
                        principalColumn: "NaploID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KulcsMozgasok_Felhasznalok_AtvevoID",
                        column: x => x.AtvevoID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KulcsMozgasok_Felhasznalok_PortasID",
                        column: x => x.PortasID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KulcsMozgasok_Kulcsok_KulcsID",
                        column: x => x.KulcsID,
                        principalTable: "Kulcsok",
                        principalColumn: "KulcsID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MesterkulcsJogosultsagok",
                columns: table => new
                {
                    JogosultsagID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ErvenyessegKezdete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErvenyessegVege = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdminisztratorID = table.Column<int>(type: "integer", nullable: false),
                    KulcsID = table.Column<int>(type: "integer", nullable: false),
                    AuditLogID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesterkulcsJogosultsagok", x => x.JogosultsagID);
                    table.ForeignKey(
                        name: "FK_MesterkulcsJogosultsagok_AuditLogok_AuditLogID",
                        column: x => x.AuditLogID,
                        principalTable: "AuditLogok",
                        principalColumn: "NaploID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MesterkulcsJogosultsagok_Felhasznalok_AdminisztratorID",
                        column: x => x.AdminisztratorID,
                        principalTable: "Felhasznalok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MesterkulcsJogosultsagok_Kulcsok_KulcsID",
                        column: x => x.KulcsID,
                        principalTable: "Kulcsok",
                        principalColumn: "KulcsID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogok_UzemeltetesiIgazgatoID",
                table: "AuditLogok",
                column: "UzemeltetesiIgazgatoID");

            migrationBuilder.CreateIndex(
                name: "IX_Foglalasok_AuditLogID",
                table: "Foglalasok",
                column: "AuditLogID");

            migrationBuilder.CreateIndex(
                name: "IX_Foglalasok_OktatoID",
                table: "Foglalasok",
                column: "OktatoID");

            migrationBuilder.CreateIndex(
                name: "IX_Foglalasok_TeremID",
                table: "Foglalasok",
                column: "TeremID");

            migrationBuilder.CreateIndex(
                name: "IX_Hibak_PortasID",
                table: "Hibak",
                column: "PortasID");

            migrationBuilder.CreateIndex(
                name: "IX_Hibak_TeremID",
                table: "Hibak",
                column: "TeremID");

            migrationBuilder.CreateIndex(
                name: "IX_KarbantartasiIdosavok_AuditLogID",
                table: "KarbantartasiIdosavok",
                column: "AuditLogID");

            migrationBuilder.CreateIndex(
                name: "IX_KarbantartasiIdosavok_TeremID",
                table: "KarbantartasiIdosavok",
                column: "TeremID");

            migrationBuilder.CreateIndex(
                name: "IX_KulcsMozgasok_AtvevoID",
                table: "KulcsMozgasok",
                column: "AtvevoID");

            migrationBuilder.CreateIndex(
                name: "IX_KulcsMozgasok_AuditLogID",
                table: "KulcsMozgasok",
                column: "AuditLogID");

            migrationBuilder.CreateIndex(
                name: "IX_KulcsMozgasok_KulcsID",
                table: "KulcsMozgasok",
                column: "KulcsID");

            migrationBuilder.CreateIndex(
                name: "IX_KulcsMozgasok_PortasID",
                table: "KulcsMozgasok",
                column: "PortasID");

            migrationBuilder.CreateIndex(
                name: "IX_Kulcsok_TeremID",
                table: "Kulcsok",
                column: "TeremID");

            migrationBuilder.CreateIndex(
                name: "IX_MesterkulcsJogosultsagok_AdminisztratorID",
                table: "MesterkulcsJogosultsagok",
                column: "AdminisztratorID");

            migrationBuilder.CreateIndex(
                name: "IX_MesterkulcsJogosultsagok_AuditLogID",
                table: "MesterkulcsJogosultsagok",
                column: "AuditLogID");

            migrationBuilder.CreateIndex(
                name: "IX_MesterkulcsJogosultsagok_KulcsID",
                table: "MesterkulcsJogosultsagok",
                column: "KulcsID");

            migrationBuilder.CreateIndex(
                name: "IX_Termek_AdminisztratorID",
                table: "Termek",
                column: "AdminisztratorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Foglalasok");

            migrationBuilder.DropTable(
                name: "Hibak");

            migrationBuilder.DropTable(
                name: "KarbantartasiIdosavok");

            migrationBuilder.DropTable(
                name: "KulcsMozgasok");

            migrationBuilder.DropTable(
                name: "MesterkulcsJogosultsagok");

            migrationBuilder.DropTable(
                name: "AuditLogok");

            migrationBuilder.DropTable(
                name: "Kulcsok");

            migrationBuilder.DropTable(
                name: "Termek");

            migrationBuilder.DropTable(
                name: "Felhasznalok");
        }
    }
}
