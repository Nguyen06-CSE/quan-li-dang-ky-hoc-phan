using QuanLyDKHP.Core.Authorization;
using QuanLyDKHP.Core.Enums;
using QuanLyDKHP.Core.Interfaces;

namespace QuanLyDKHP.Services;

/// <summary>
/// Triển khai ICurrentUserService — Singleton lưu thông tin user đang đăng nhập.
/// Thread-safe cho các thao tác set/clear.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly object _lock = new();
    private CurrentUserInfo? _currentUser;

    /// <inheritdoc />
    public CurrentUserInfo? CurrentUser
    {
        get
        {
            lock (_lock)
            {
                return _currentUser;
            }
        }
    }

    /// <inheritdoc />
    public bool IsInRole(UserRole role)
    {
        var user = CurrentUser;
        return user != null && user.Role == role;
    }

    /// <inheritdoc />
    public bool HasPermission(string chucNang)
    {
        var user = CurrentUser;
        if (user == null) return false;
        return PermissionMatrix.HasPermission(chucNang, user.Role);
    }

    /// <inheritdoc />
    public void SetCurrentUser(CurrentUserInfo user)
    {
        lock (_lock)
        {
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    /// <inheritdoc />
    public void ClearCurrentUser()
    {
        lock (_lock)
        {
            _currentUser = null;
        }
    }
}
