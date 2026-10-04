// src/QuanLyDKHP.App/Helpers/VietnameseNameComparer.cs

using System;
using System.Collections;
using QuanLyDKHP.Core.Dtos;

namespace QuanLyDKHP.App.Helpers;

public class VietnameseNameComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        // Avalonia DataGrid truyền vào 2 object của cả dòng (SinhVienDto)
        string name1 = x is SinhVienDto sv1 ? (sv1.HoTen ?? "") : "";
        string name2 = y is SinhVienDto sv2 ? (sv2.HoTen ?? "") : "";

        var parts1 = ExtractNameParts(name1);
        var parts2 = ExtractNameParts(name2);

        // 1. So sánh Tên (Ưu tiên cao nhất)
        int nameCmp = string.Compare(parts1.Ten, parts2.Ten, StringComparison.CurrentCultureIgnoreCase);
        if (nameCmp != 0) return nameCmp;

        // 2. Nếu Tên giống nhau -> So sánh Tên lót
        int middleCmp = string.Compare(parts1.TenLot, parts2.TenLot, StringComparison.CurrentCultureIgnoreCase);
        if (middleCmp != 0) return middleCmp;

        // 3. Nếu Tên & Lót giống nhau -> So sánh Họ
        return string.Compare(parts1.Ho, parts2.Ho, StringComparison.CurrentCultureIgnoreCase);
    }

    private (string Ho, string TenLot, string Ten) ExtractNameParts(string fullName)
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