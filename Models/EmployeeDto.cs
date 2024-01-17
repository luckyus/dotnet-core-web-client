using System.Text.Json.Serialization;
using System;

namespace dotnet_core_web_client.Models
{
	public record EmployeeDto
	{
		[JsonPropertyName("employeeId")]
		public string EmployeeId { get; set; }
		[JsonPropertyName("originalId")]
		public string OriginalId { get; set; } = null;
		[JsonPropertyName("lastName")]
		public string LastName { get; set; }
		[JsonPropertyName("firstName")]
		public string FirstName { get; set; }
		[JsonPropertyName("otherName")]
		public string OtherName { get; set; }
		[JsonPropertyName("email")]
		public string Email { get; set; }
		[JsonPropertyName("isActive")]
		public bool IsActive { get; set; }
		[JsonPropertyName("isAutoMatch")]
		public bool IsAutoMatch { get; set; }
		[JsonPropertyName("isPassword")]
		public bool IsPassword { get; set; }
		[JsonPropertyName("password")]
		public string Password { get; set; }
		[JsonPropertyName("departments")]
		public string[] Departments { get; set; } = [];
		[JsonPropertyName("base64Image")]
		public byte[] Base64Image { get; set; } = [];
		[JsonPropertyName("fingerPrints")]
		public FingerPrint[] Fingerprints { get; set; } = new FingerPrint[2];
		[JsonPropertyName("isFingerprint")]
		public bool IsFingerprint { get; set; }
		[JsonPropertyName("modifiedDate")]
		public DateTime? ModifiedDate { get; set; } = null;
		[JsonPropertyName("smartCardSN")]
		public long SmartCardSN { get; set; }

		// the following not incluced in existing iGuard530, so set to null to exclude them (220208)
		[JsonPropertyName("internalId")]
		public string InternalId { get; set; } = null;
		[JsonPropertyName("startDate")]
		public DateTime? StartDate { get; set; } = null;
		[JsonPropertyName("probationDate")]
		public DateTime? ProbationDate { get; set; } = null;
		[JsonPropertyName("terminatedDate")]
		public DateTime? TerminatedDate { get; set; } = null;
		[JsonPropertyName("address1")]
		public string Address1 { get; set; } = null;
		[JsonPropertyName("address2")]
		public string Address2 { get; set; } = null;
		[JsonPropertyName("city")]
		public string City { get; set; } = null;
		[JsonPropertyName("state")]
		public string State { get; set; } = null;
		[JsonPropertyName("zip")]
		public string Zip { get; set; } = null;
		[JsonPropertyName("country")]
		public string Country { get; set; } = null;
		[JsonPropertyName("homePhone")]
		public string HomePhone { get; set; } = null;
		[JsonPropertyName("cellPhone")]
		public string CellPhone { get; set; } = null;
		[JsonPropertyName("idNumber")]
		public string IDNumber { get; set; } = null;
		[JsonPropertyName("bankName")]
		public string BankName { get; set; } = null;

		// for internal use for the Fingerprints[2] above (220208)
		public class FingerPrint
		{
			public int iNumMinu { get; set; }
			public byte[] pMinutiae { get; set; }
		}
	}
}
