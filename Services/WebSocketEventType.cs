namespace dotnet_core_web_client.Services
{
	public static class WebSocketEventType
	{
		// department CRUD (240222)
		public const string AddDepartment = "AddDepartment";
		public const string GetDepartment = "GetDepartment";
		public const string UpdateDepartment = "UpdateDepartment";
		public const string DeleteDepartment = "DeleteDepartment";

		// quick access (240305)
		public const string QuickAccess = "QuickAccess";
		public const string GetQuickAccess = "GetQuickAccess";
		public const string UpdateQuickAccess = "UpdateQuickAccess";

		// employee CRUD (230911)
		public const string GetEmployee = "GetEmployee";
		public const string AddEmployee = "AddEmployees";
		public const string UpdateEmployee = "UpdateEmployees";
		public const string DeleteEmployee = "DeleteEmployee";
		public const string RequestInsertPermission = "RequestInsertPermission";

		// access log (230220)
		public const string Accesslogs = "Accesslogs";
		public const string GetInOutStatus = "GetInOutStatus";
		public const string GetAccessRight = "GetAccessRight";
		public const string OnAccessLogSync = "OnAccessLogSync";
		public const string AccessLogSync = "AccessLogSync";
	}
}
