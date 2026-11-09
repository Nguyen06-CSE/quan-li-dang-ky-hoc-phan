// src/QuanLyDKHP.App/Helpers/VietnameseNameComparer.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.App.Helpers;

public class VietnameseNameComparer : IComparer, IComparer<SinhVienDto>, IComparer<string>
{
    public static readonly VietnameseNameComparer Instance = new();

    private static readonly StringComparer ViStringComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), ignoreCase: true);

    public int Compare(object? x, object? y)
    {
        if (x is SinhVienDto sv1 && y is SinhVienDto sv2)
            return Compare(sv1, sv2);

        string name1 = x as string ?? "";
        string name2 = y as string ?? "";
        return Compare(name1, name2);
    }

    public int Compare(SinhVienDto? x, SinhVienDto? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int cmp = Compare(x.HoTen, y.HoTen);
        if (cmp != 0) return cmp;

        return ViStringComparer.Compare(x.MaSV ?? "", y.MaSV ?? "");
    }

    public int Compare(string? name1, string? name2)
    {
        var parts1 = ExtractNameParts(name1 ?? "");
        var parts2 = ExtractNameParts(name2 ?? "");

        // 1. So sánh Tên (Ưu tiên cao nhất) theo chuẩn tiếng Việt
        int nameCmp = ViStringComparer.Compare(parts1.Ten, parts2.Ten);
        if (nameCmp != 0) return nameCmp;

        // 2. Nếu Tên giống nhau -> So sánh Tên lót
        int middleCmp = ViStringComparer.Compare(parts1.TenLot, parts2.TenLot);
        if (middleCmp != 0) return middleCmp;

        // 3. Nếu Tên & Lót giống nhau -> So sánh Họ
        return ViStringComparer.Compare(parts1.Ho, parts2.Ho);
    }

    private static (string Ho, string TenLot, string Ten) ExtractNameParts(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) 
            return ("", "", "");

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 1) 
            return ("", "", parts[0]);

        string ho = parts[0];
        string ten = parts[^1]; // Lấy phần tử cuối cùng
        
        // Nếu tên có 3 chữ trở lên, ghép các chữ ở giữa làm tên lót
        string tenLot = parts.Length > 2 ? string.Join(" ", parts[1..^1]) : ""; 

        return (ho, tenLot, ten);
    }
}
