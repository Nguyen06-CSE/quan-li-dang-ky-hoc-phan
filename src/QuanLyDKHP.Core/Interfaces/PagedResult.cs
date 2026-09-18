using System.Collections.Generic;

namespace QuanLyDKHP.Core.Interfaces;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)System.Math.Ceiling((double)TotalItems / PageSize);
}
