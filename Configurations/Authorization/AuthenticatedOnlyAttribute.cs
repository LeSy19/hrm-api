using Microsoft.AspNetCore.Mvc.Controllers;

namespace BackendApp.Configurations.Authorization;

/// <summary>
/// Đánh dấu endpoint chỉ cần đăng nhập, không cần phân quyền
/// (không xuất hiện trong danh sách phân quyền).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthenticatedOnlyAttribute : Attribute { }

public static class PermissionKey
{
    public static string From(ControllerActionDescriptor d) => $"{d.ControllerName}.{d.ActionName}";
}