// namespace BackendApp.constants.permissions;

// public class ApiRouteDefinition
// {
//     public string HttpMethod { get; set; } = string.Empty; // "GET", "POST", "PUT", "DELETE"
//     public string RouteTemplate { get; set; } = string.Empty; // "api/employee", "api/employee/{id}"
// }

// public class PermissionDefinition
// {
//     public string Code { get; }
//     public string Name { get; }
//     public string Description { get; }
//     public string Module { get; }
//     public string Action { get; }
//     public int DisplayOrder { get; }
//     public List<ApiRouteDefinition> ApiRoutes { get; } // Danh sách API Endpoint được bảo vệ bởi Permission này

//     public PermissionDefinition(
//         string code,
//         string name,
//         string module,
//         string action,
//         string description = "",
//         int displayOrder = 0,
//         List<ApiRouteDefinition>? apiRoutes = null)
//     {
//         Code = code;
//         Name = name;
//         Module = module;
//         Action = action;
//         Description = description;
//         DisplayOrder = displayOrder;
//         ApiRoutes = apiRoutes ?? new List<ApiRouteDefinition>();
//     }
// }