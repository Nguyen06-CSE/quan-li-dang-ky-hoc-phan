using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDKHP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHocPhiHocKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HocPhiHocKy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MaSV = table.Column<string>(type: "varchar(20)", nullable: false),
                    MaHocKy = table.Column<string>(type: "varchar(20)", nullable: false),
                    TongSoTinChi = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TongHocPhi = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    DaDong = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    ConNo = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    DaKhoaSo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    NgayKhoaSo = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocPhiHocKy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HocPhiHocKy_HocKy_MaHocKy",
                        column: x => x.MaHocKy,
                        principalTable: "HocKy",
                        principalColumn: "MaHocKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HocPhiHocKy_SinhVien_MaSV",
                        column: x => x.MaSV,
                        principalTable: "SinhVien",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HocPhiHocKy_MaHocKy",
                table: "HocPhiHocKy",
                column: "MaHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhiHocKy_MaSV_MaHocKy",
                table: "HocPhiHocKy",
                columns: new[] { "MaSV", "MaHocKy" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HocPhiHocKy");
        }
    }
}
