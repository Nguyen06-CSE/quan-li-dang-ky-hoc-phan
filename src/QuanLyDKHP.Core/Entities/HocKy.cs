using System;
using System.Collections.Generic;

namespace QuanLyDKHP.Core.Entities;

public class HocKy : IAuditableEntity, IEquatable<HocKy>
{
    public string MaHocKy { get; set; } = string.Empty;
    public string TenHocKy { get; set; } = string.Empty;
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool DangMo { get; set; } = true;
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }

    public ICollection<LopHocPhan> LopHocPhans { get; set; } = new List<LopHocPhan>();

    public bool Equals(HocKy? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(MaHocKy, other.MaHocKy, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as HocKy);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(MaHocKy ?? string.Empty);
    }

    public static bool operator ==(HocKy? left, HocKy? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(HocKy? left, HocKy? right)
    {
        return !(left == right);
    }

    public override string ToString() => TenHocKy;
}
