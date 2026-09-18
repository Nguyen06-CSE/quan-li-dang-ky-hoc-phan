namespace QuanLyDKHP.Core.Entities;

public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }
}
