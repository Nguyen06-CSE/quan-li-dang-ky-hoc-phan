using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyDKHP.Core.Entities;

namespace QuanLyDKHP.Core.Interfaces;

public interface IMemoryCacheStore
{
    IReadOnlyList<HocKy> DanhSachHocKy { get; }
    IReadOnlyList<string> DanhSachLopSinhHoat { get; }
    IReadOnlyList<string> DanhSachKhoaHoc { get; }
    IReadOnlyList<MonHoc> DanhSachMonHoc { get; }

    bool IsInitialized { get; }
    Task InitializeAsync(bool forceReload = false);
}