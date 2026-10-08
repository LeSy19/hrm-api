
// using BackendApp.constants.permissions;

// namespace BackendApp.constants;

// public static class PermissionCatalog
// {
//     public static class Modules
//     {
//         public const string Dashboard = "Dashboard";
//         public const string Employee = "Employee";
//         public const string Department = "Department";
//         public const string LeaveRequest = "LeaveRequest";
//         public const string LeaveBalance = "LeaveBalance";
//         public const string RolePermission = "RolePermission";
//     }

//     public static readonly List<PermissionDefinition> AllPermissions = new()
//     {
//         // ================= EMPLOYEE MODULE =================
//         new("Employee.View", "Xem danh sách nhân viên", Modules.Employee, "View", "Xem thông tin nhân viên", 10,
//             new() {
//                 new() { HttpMethod = "GET", RouteTemplate = "api/employee" },
//                 new() { HttpMethod = "GET", RouteTemplate = "api/employee/{id}" }
//             }),

//         new("Employee.Create", "Tạo mới nhân viên", Modules.Employee, "Create", "Thêm hồ sơ nhân viên mới", 11,
//             new() {
//                 new() { HttpMethod = "POST", RouteTemplate = "api/employee" }
//             }),

//         new("Employee.Update", "Cập nhật nhân viên", Modules.Employee, "Update", "Sửa thông tin hồ sơ nhân viên", 12,
//             new() {
//                 new() { HttpMethod = "PUT", RouteTemplate = "api/employee/{id}" }
//             }),

//         new("Employee.Delete", "Xóa nhân viên", Modules.Employee, "Delete", "Xóa hồ sơ nhân viên", 13,
//             new() {
//                 new() { HttpMethod = "DELETE", RouteTemplate = "api/employee/{id}" }
//             }),

//         // ================= DEPARTMENT MODULE =================
//         new("Department.View", "Xem danh sách phòng ban", Modules.Department, "View", "Xem thông tin phòng ban", 14,
//             new() {
//                 new() { HttpMethod = "GET", RouteTemplate = "api/department" },
//                 new() { HttpMethod = "GET", RouteTemplate = "api/department/{id}" }
//             }),

//         new("Department.Create", "Tạo mới phòng ban", Modules.Department, "Create", "Thêm hồ sơ phòng ban mới", 15,
//             new() {
//                 new() { HttpMethod = "POST", RouteTemplate = "api/department" }
//             }),

//         new("Department.Update", "Cập nhật phòng ban", Modules.Department, "Update", "Sửa thông tin hồ sơ phòng ban", 16,
//             new() {
//                 new() { HttpMethod = "PUT", RouteTemplate = "api/department/{id}" }
//             }),

//         new("Department.Delete", "Xóa phòng ban", Modules.Department, "Delete", "Xóa hồ sơ phòng ban", 17,
//             new() {
//                 new() { HttpMethod = "DELETE", RouteTemplate = "api/department/{id}" }
//             }),


//         // ================= LEAVE REQUEST MODULE =================
//         new("LeaveRequest.View", "Xem đơn nghỉ phép", Modules.LeaveRequest, "View", "Xem danh sách đơn xin nghỉ", 20,
//             new() {
//                 new() { HttpMethod = "GET", RouteTemplate = "api/leaverequest" },
//                 new() { HttpMethod = "GET", RouteTemplate = "api/leaverequest/{id}" }
//             }),

//         new("LeaveRequest.Create", "Tạo đơn nghỉ phép", Modules.LeaveRequest, "Create", "Đăng ký đơn nghỉ phép", 21,
//             new() {
//                 new() { HttpMethod = "POST", RouteTemplate = "api/leaverequest" }
//             }),

//         new("LeaveRequest.Approve", "Duyệt đơn nghỉ phép", Modules.LeaveRequest, "Approve", "Phê duyệt đơn nghỉ phép", 22,
//             new() {
//                 new() { HttpMethod = "POST", RouteTemplate = "api/leaverequest/approve/{id}" }
//             }),

//         new("LeaveRequest.Reject", "Từ chối đơn nghỉ phép", Modules.LeaveRequest, "Reject", "Từ chối đơn nghỉ phép", 23,
//             new() {
//                 new() { HttpMethod = "POST", RouteTemplate = "api/leaverequest/reject/{id}" }
//             }),

//         // ================= ROLE & PERMISSION MODULE =================
//         new("RolePermission.View", "Xem phân quyền", Modules.RolePermission, "View", "Xem danh sách Role & Permission", 90,
//             new() {
//                 new() { HttpMethod = "GET", RouteTemplate = "api/rolepermission/permissions" },
//                 new() { HttpMethod = "GET", RouteTemplate = "api/rolepermission/roles/{roleid}/permissions" }
//             }),

//         new("RolePermission.Update", "Cập nhật phân quyền", Modules.RolePermission, "Update", "Gán/bỏ quyền cho Role", 91,
//             new() {
//                 new() { HttpMethod = "PUT", RouteTemplate = "api/rolepermission/roles/{roleid}/permissions" }
//             })
//     };

//     // Helper lookup tối ưu tốc độ tra cứu: Key = "POST api/employee" -> Value = "Employee.Create"
//     private static readonly Lazy<Dictionary<string, string>> RouteToPermissionMap = new(() =>
//     {
//         var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
//         foreach (var perm in AllPermissions)
//         {
//             foreach (var route in perm.ApiRoutes)
//             {
//                 var key = $"{route.HttpMethod.ToUpper()} {route.RouteTemplate.Trim('/').ToLower()}";
//                 map[key] = perm.Code;
//             }
//         }
//         return map;
//     });

//     public static string? GetRequiredPermission(string httpMethod, string routeTemplate)
//     {
//         var key = $"{httpMethod.ToUpper()} {routeTemplate.Trim('/').ToLower()}";
//         return RouteToPermissionMap.Value.TryGetValue(key, out var code) ? code : null;
//     }
// }