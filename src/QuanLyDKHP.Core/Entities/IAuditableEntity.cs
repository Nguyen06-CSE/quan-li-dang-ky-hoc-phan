using System;

namespace QuanLyDKHP.Core.Entities;

public interface IAuditableEntity
{
    DateTime NgayTao { get; set; }
    DateTime? NgayCapNhat { get; set; }
}
