using System;

namespace QuanLyDKHP.Core.Entities;

public class CauHinhHeThong : IAuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
}
