using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyDKHP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauHinhHeThong",
                columns: table => new
                {
                    Key = table.Column<string>(type: "varchar(50)", nullable: false),
                    Value = table.Column<string>(type: "varchar(50)", nullable: false),
                    MoTa = table.Column<string>(type: "varchar(200)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhHeThong", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "HocKy",
                columns: table => new
                {
                    MaHocKy = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenHocKy = table.Column<string>(type: "varchar(50)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "date", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "date", nullable: true),
                    DangMo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocKy", x => x.MaHocKy);
                });

            migrationBuilder.CreateTable(
                name: "MonHoc",
                columns: table => new
                {
                    MaMon = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenMon = table.Column<string>(type: "varchar(150)", nullable: false),
                    SoTinChiLT = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SoTinChiTH = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BacDaoTao = table.Column<string>(type: "varchar(10)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHoc", x => x.MaMon);
                    table.CheckConstraint("CK_MonHoc_SoTinChi", "\"SoTinChiLT\" + \"SoTinChiTH\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", nullable: false),
                    MatKhauHash = table.Column<string>(type: "varchar(255)", nullable: false),
                    HoTen = table.Column<string>(type: "varchar(100)", nullable: false),
                    Role = table.Column<string>(type: "varchar(30)", nullable: false),
                    MaGV = table.Column<string>(type: "varchar(20)", nullable: true),
                    TrangThai = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "HoatDong"),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SinhVien",
                columns: table => new
                {
                    MaSV = table.Column<string>(type: "varchar(20)", nullable: false),
                    HoTen = table.Column<string>(type: "varchar(100)", nullable: false),
                    LopSinhHoat = table.Column<string>(type: "varchar(30)", nullable: true),
                    KhoaHoc = table.Column<string>(type: "varchar(20)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinhVien", x => x.MaSV);
                });

            migrationBuilder.CreateTable(
                name: "LopHocPhan",
                columns: table => new
                {
                    MaLHP = table.Column<string>(type: "varchar(30)", nullable: false),
                    MaMon = table.Column<string>(type: "varchar(20)", nullable: false),
                    MaHocKy = table.Column<string>(type: "varchar(20)", nullable: false),
                    MaGV = table.Column<string>(type: "varchar(20)", nullable: true),
                    LoaiHinhDT = table.Column<string>(type: "varchar(10)", nullable: true),
                    SiSoToiDa = table.Column<int>(type: "integer", nullable: true),
                    GiangDayOnline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocPhan", x => x.MaLHP);
                    table.ForeignKey(
                        name: "FK_LopHocPhan_HocKy_MaHocKy",
                        column: x => x.MaHocKy,
                        principalTable: "HocKy",
                        principalColumn: "MaHocKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LopHocPhan_MonHoc_MaMon",
                        column: x => x.MaMon,
                        principalTable: "MonHoc",
                        principalColumn: "MaMon",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DangKyHocPhan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MaSV = table.Column<string>(type: "varchar(20)", nullable: false),
                    MaLHP = table.Column<string>(type: "varchar(30)", nullable: false),
                    HinhThucDK = table.Column<string>(type: "varchar(10)", nullable: true),
                    NgayDK = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NguoiDK = table.Column<string>(type: "varchar(50)", nullable: true),
                    DiemSo = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    DiemChu = table.Column<string>(type: "varchar(5)", nullable: true),
                    TrangThai = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "DangHoc"),
                    SoTienPhaiDong = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    SoTienDaDong = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    NgayTao = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    NgayCapNhat = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyHocPhan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DangKyHocPhan_LopHocPhan_MaLHP",
                        column: x => x.MaLHP,
                        principalTable: "LopHocPhan",
                        principalColumn: "MaLHP",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DangKyHocPhan_SinhVien_MaSV",
                        column: x => x.MaSV,
                        principalTable: "SinhVien",
                        principalColumn: "MaSV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CauHinhHeThong",
                columns: new[] { "Key", "MoTa", "NgayCapNhat", "NgayTao", "Value" },
                values: new object[,]
                {
                    { "DonGiaTinChiLT", "Đơn giá 1 TC lý thuyết (VNĐ)", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "500000" },
                    { "DonGiaTinChiTH", "Đơn giá 1 TC thực hành (VNĐ)", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "700000" },
                    { "SoTinChiToiDa", "Số TC tối đa/học kỳ", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "25" },
                    { "SoTinChiToiThieu", "Số TC tối thiểu/học kỳ", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "10" }
                });

            migrationBuilder.InsertData(
                table: "NguoiDung",
                columns: new[] { "Id", "HoTen", "MaGV", "MatKhauHash", "NgayCapNhat", "NgayTao", "Role", "TenDangNhap", "TrangThai" },
                values: new object[] { 1, "Quản trị viên", null, "$2a$11$jZTP6M2fUm3QlomDlyrWIO6IRd8BeVrcyOhY/wlXprA1A8iFII/LO", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", "admin", "HoatDong" });

            migrationBuilder.CreateIndex(
                name: "IX_DangKyHocPhan_MaLHP",
                table: "DangKyHocPhan",
                column: "MaLHP");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyHocPhan_MaSV_MaLHP",
                table: "DangKyHocPhan",
                columns: new[] { "MaSV", "MaLHP" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhan_MaHocKy",
                table: "LopHocPhan",
                column: "MaHocKy");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhan_MaMon_MaHocKy",
                table: "LopHocPhan",
                columns: new[] { "MaMon", "MaHocKy" });

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_TenDangNhap",
                table: "NguoiDung",
                column: "TenDangNhap",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CauHinhHeThong");

            migrationBuilder.DropTable(
                name: "DangKyHocPhan");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "LopHocPhan");

            migrationBuilder.DropTable(
                name: "SinhVien");

            migrationBuilder.DropTable(
                name: "HocKy");

            migrationBuilder.DropTable(
                name: "MonHoc");
        }
    }
}
