using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace dotnet_core_web_client.Models
{
	public class DepartmentDto
	{
		[JsonPropertyName("departmentId")]
		public string DeptId { get; set; }
		[JsonPropertyName("name")]
		public string DeptName { get; set; }
		[JsonPropertyName("terminalId")]
		public string[] TerminalIds { get; set; }
		[JsonPropertyName("timeRestrictions")]
		public bool[][][] TimeRestrictions { get; set; }
	}
}
