using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using dotnet_core_web_client.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_core_web_client.Services
{
	public class WebSocketHandler(ITerminalSettingsRepository terminalSettingsRepository, ITerminalRepository terminalRepository, INetworkRepository networkRepository, IMemoryCache memoryCache) : IWebSocketHandler
	{
		protected WebSocket webSocket;
		protected ClientWebSocketHandler clientWebSocketHandler = null;
		readonly string smartCardSNConfigPath = Directory.GetCurrentDirectory() + "/DBase/smartCardSN.json";

		// to be assigned in onConnectClick eventType (230709)
		protected string sn;

		// private readonly IServiceScopeFactory _scopeFactory;
		public ITerminalSettingsRepository terminalSettingsRepository = terminalSettingsRepository;
		public ITerminalRepository terminalRepository = terminalRepository;
		public INetworkRepository networkRepository = networkRepository;

		// cache (231228)
		protected IMemoryCache memoryCache = memoryCache;

		protected JsonSerializerOptions jsonSerializerOptionsIgnoreNull = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

		public void OnConnected(WebSocket webSocket)
		{
			this.webSocket = webSocket;
		}

		/*
		private void SaveTerminal()
		{
			string jsonStr = JsonSerializer.Serialize<Terminal>(Terminal, new JsonSerializerOptions { IgnoreNullValues = true });
			try
			{
				File.WriteAllText(terminalConfigPath, jsonStr);
			}
			catch (Exception ex)
			{
				_ = ex.Message;
			}
		}
		*/

		public async Task SendAsync(string message)
		{
			if (webSocket.State == WebSocketState.Open)
			{
				var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
				await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			return;
		}

		public async Task ReceiveAsync()
		{
			var receiveBuffer = new ArraySegment<byte>(new byte[100]);

			while (webSocket.State == WebSocketState.Open)
			{
				using var ms = new MemoryStream();
				WebSocketReceiveResult result;

				try
				{
					do
					{
						result = await webSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
						ms.Write(receiveBuffer.Array, receiveBuffer.Offset, result.Count);
					} while (!result.EndOfMessage);

					ms.Seek(0, SeekOrigin.Begin);

					if (result.MessageType == WebSocketMessageType.Text)
					{
						using var reader = new StreamReader(ms, Encoding.UTF8);
						string jsonStr = reader.ReadToEnd();
						var jsonObj = JsonSerializer.Deserialize<WebSocketMessage>(jsonStr);

						string eventType = jsonObj.EventType;

						if (eventType == "onConnectClick")
						{
							// get the smartCard sn array (210127)
							string smartCardSNJsonStr = File.ReadAllText(smartCardSNConfigPath);
							var smartCardSNJsonElement = JsonSerializer.Deserialize<JsonElement>(smartCardSNJsonStr) as JsonElement?;
							var smartCardSNArray = smartCardSNJsonElement?.GetProperty("smartCardSNArray");

							// array of objects (201103)
							object[] data = ["iGuardPayroll Connecting...", smartCardSNArray];
							var obj = new { eventType = "onConnecting", data };
							var str = JsonSerializer.Serialize(obj);
							_ = SendAsync(str);

							// update the existing terminal info if necessary (these are the two inputs b4 the 'connect' btn) (201201)
							// - now include regCode (221019)
							var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonObj.Data[0].ToString()) as JsonElement?;

							sn = jsonElement?.GetProperty("SN").GetString();
							string iGuardPayrollIpPort = jsonElement?.GetProperty("IpPort").GetString();
							string regCode = jsonElement?.GetProperty("RegCode").GetString();
							bool isNoTimeStamp = jsonElement?.GetProperty("IsNoTimeStamp").GetBoolean() ?? false;

							//if (Terminal.SN != sn)
							//{
							//    Terminal.SN = sn;
							//    SaveTerminal();
							//}

							// connect to iGuardPayroll (201201)
							clientWebSocketHandler = new ClientWebSocketHandler(this, sn, iGuardPayrollIpPort, regCode, isNoTimeStamp, memoryCache);
							_ = clientWebSocketHandler.Initialize();
						}
						else if (eventType == "accessLog")
						{
							await AccessLog(jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.Accesslogs)
						{
							await AccessLogs(jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.GetAccessRight)
						{
							await GetAccessRight(jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.RequestInsertPermission)
						{
							await RequestInsertPermissionAsync(jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.GetEmployee)
						{
							await GetEmployeeAsync(jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.AddEmployee || eventType == WebSocketEventType.UpdateEmployee)
						{
							await SetEmployeeAsync(eventType, jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.AddDepartment || eventType == WebSocketEventType.UpdateDepartment)
						{
							await SetDepartmentAsync(eventType, jsonObj.Data);
						}
						else if (eventType == WebSocketEventType.UpdateQuickAccess)
						{
							await UpdateQuickAccessAsync(jsonObj.Data);
						}
						else if (eventType == "onTestClick")
						{
							OnTestClick();
						}
						else
						{
							if (clientWebSocketHandler != null)
							{
								await DefaultEventTypeAsync(eventType, jsonObj.Data);
							}
						}
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						if (clientWebSocketHandler != null)
						{
							try { await clientWebSocketHandler.CloseAsync(); }
							catch { /* who cares? */ }
						}
						await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
					}
				}
				catch (WebSocketException)
				{
					if (clientWebSocketHandler != null) await clientWebSocketHandler.CloseAsync();
				}
			}

			return;
		}

		private void OnTestClick()
		{
			string ConvertHexSerialNo(uint hsn, bool IsTwoDigitPrefix)
			{
				if (IsTwoDigitPrefix)
				{
					uint p = (hsn & 0xFF000000) >> 24;  // Prefix
					uint n = (hsn & 0x00FFFFFF) >> 4;       // SN
					uint cs = hsn & 0xF;                // Checksum
					string s = $"{p:X2}{n:D9}{cs}";           // Prefix + SN + CS

					s = s.Insert(8, "-");
					s = s.Insert(4, "-");

					return s;
				}
				else
				{
					uint p = (hsn & 0xF0000000) >> 28;  // Prefix
					uint n = (hsn & 0x0FFFFFFF) >> 4;       // SN
					uint cs = hsn & 0xF;                // Checksum
					string s = $"{p}{n:D10}{cs}";           // Prefix + SN + CS

					s = s.Insert(8, "-");
					s = s.Insert(4, "-");

					return s;
				}
			}

			uint ConvertSerialNo(string sn, bool IsTwoDigitPrefix)
			{
				uint rc;
				uint cs;
				uint prefix;
				uint prefix2;

				if (IsTwoDigitPrefix)
				{
					sn = sn.Replace("-", "");
					cs = sn[sn.Length - 1];                 // last digit is checksum
					prefix = sn[0];                         // first digit prefix
					prefix2 = sn[1];
					sn = sn.Substring(2, sn.Length - 3);    // remove first and last digit

					if (uint.TryParse(sn, out rc))
					{
						rc = rc << 4 & 0xFFFFFF;   // shift one digit and remove MS digit
						rc |= cs - '0';
						rc |= prefix - '0' << 28;
						rc |= prefix2 - '0' << 24;
					}
					else
					{
						rc = uint.MaxValue;
					}
				}
				else
				{
					sn = sn.Replace("-", "");
					cs = sn[sn.Length - 1];                 // last digit is checksum
					prefix = sn[0];                         // first digit prefix
					sn = sn.Substring(1, sn.Length - 2);    // remove first and last digit

					if (uint.TryParse(sn, out rc))
					{
						rc = rc << 4 & 0xFFFFFFF;   // shift one digit and remove MS digit
						rc |= cs - '0';
						rc |= prefix - '0' << 28;
					}
					else
					{
						rc = uint.MaxValue;
					}
				}

				return rc;
			}

			// convert sn fm string to int (iGuardPayroll uses int to store sn) (201230)
			uint myConvertSerialNo(string sn)
			{
				uint iSn = 0;
				MachineType machineType = MachineType.iGuardExpress;

				if (sn.Length == 14)
				{
					string snPrefix = sn.Substring(0, 2);

					if (snPrefix == "70") machineType = MachineType.iGuardExpress;
					else if (snPrefix == "71") machineType = MachineType.iGuardExpress540;
					else if (snPrefix == "80") machineType = MachineType.iGuard530;
					else if (snPrefix == "81") machineType = MachineType.iGuard540;

					if (machineType == MachineType.iGuardExpress || machineType == MachineType.iGuard530)
					{
						string s1 = sn.Substring(1, 12).Replace("-", "");   // eg. 7000-0000-0355 -> 1879048757 (150417)
						uint i1 = uint.Parse(s1) * 16;
						string s2 = sn.Substring(13, 1);
						uint i2 = uint.Parse(s2);

						if (machineType == MachineType.iGuardExpress)
						{
							iSn = (i1 + i2 + 0x70000000);
						}
						else
						{
							iSn = (i1 + i2 + 0x80000000);
						}
					}
					else
					{
						sn = sn.Replace("-", "");
						uint cs = sn[sn.Length - 1];            // last digit is checksum
						uint prefix = sn[0];                    // first digit prefix
						uint prefix2 = sn[1];
						sn = sn.Substring(2, sn.Length - 3);    // remove first, 2nd and last digit

						if (uint.TryParse(sn, out iSn))
						{
							iSn = iSn << 4 & 0xFFFFFF;   // shift one digit and remove MS digit
							iSn |= cs - '0';
							iSn |= prefix - '0' << 28;
							iSn |= prefix2 - '0' << 24;
						}
						else
						{
							iSn = uint.MaxValue;
						}
					}
				}

				return iSn;
			}

			(string, string) myConvertHexSerialNo(uint hsn)
			{
				string s1, s2;

				uint p = (hsn & 0xF0000000) >> 28;  // Prefix
				uint n = (hsn & 0x0FFFFFFF) >> 4;       // SN
				uint cs = hsn & 0xF;                // Checksum
				s1 = $"{p}{n:D10}{cs}";           // Prefix + SN + CS

				s1 = s1.Insert(8, "-");
				s1 = s1.Insert(4, "-");

				p = (hsn & 0xFF000000) >> 24;  // Prefix
				n = (hsn & 0x00FFFFFF) >> 4;       // SN
				cs = hsn & 0xF;                // Checksum
				s2 = $"{p:X2}{n:D9}{cs}";           // Prefix + SN + CS

				s2 = s2.Insert(8, "-");
				s2 = s2.Insert(4, "-");

				return (s1, s2);
			}

			string sn = "7000-0000-0355";

			uint int1 = ConvertSerialNo(sn, IsTwoDigitPrefix: false);
			string sn1 = ConvertHexSerialNo(int1, IsTwoDigitPrefix: false);

			uint int2 = ConvertSerialNo(sn, IsTwoDigitPrefix: false);
			string sn2 = ConvertHexSerialNo(int2, IsTwoDigitPrefix: true);

			uint int3 = ConvertSerialNo(sn, IsTwoDigitPrefix: true);
			string sn3 = ConvertHexSerialNo(int3, IsTwoDigitPrefix: false);

			uint int4 = ConvertSerialNo(sn, IsTwoDigitPrefix: true);
			string sn4 = ConvertHexSerialNo(int4, IsTwoDigitPrefix: true);

			uint int5 = myConvertSerialNo(sn);
			string sn5 = ConvertHexSerialNo(int5, IsTwoDigitPrefix: false);
			string sn6 = ConvertHexSerialNo(int5, IsTwoDigitPrefix: true);
			(string sn7, string sn8) = myConvertHexSerialNo(int5);
		}

		private async Task UpdateQuickAccessAsync(object[] data)
		{
			var id = Guid.NewGuid();
			string[] terminalIds = [];

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;

			var timeslot = jsonElement?.GetProperty("timeslot").GetString();

			if (jsonElement?.TryGetProperty("terminals", out var tempElement) == true)
			{
				if (tempElement.ValueKind == JsonValueKind.Array)
				{
					terminalIds = tempElement.EnumerateArray().Select(x => x.GetString()).ToArray();
				}
			}

			bool[][][] timeRestrictions = null;
			int[][][] timeRestrictions2 = null;

			if (!string.IsNullOrEmpty(timeslot))
			{
				timeRestrictions = new bool[8][][];
				timeRestrictions2 = new int[8][][];

				for (int i = 0; i < 8; i++)
				{
					timeRestrictions[i] = new bool[24][];
					timeRestrictions2[i] = new int[24][];

					for (int j = 0; j < 24; j++)
					{
						timeRestrictions[i][j] = new bool[2];
						timeRestrictions2[i][j] = new int[2];
					}
				}

				if (timeslot == "none")
				{
				}
				else if (timeslot == "ninetofiveeveryday")
				{
					for (int i = 0; i < 8; i++)
					{
						for (int j = 9; j < 18; j++)
						{
							timeRestrictions[i][j][0] = true;
							timeRestrictions[i][j][1] = true;

							timeRestrictions2[i][j][0] = 1;
							timeRestrictions2[i][j][1] = 1;
						}
					}
				}
				else if (timeslot == "ninetofiveweekdays")
				{
					for (int i = 1; i < 6; i++)
					{
						for (int j = 9; j < 18; j++)
						{
							if (i >= 1 && i <= 5)
							{
								timeRestrictions[i][j][0] = true;
								timeRestrictions[i][j][1] = true;

								timeRestrictions2[i][j][0] = 1;
								timeRestrictions2[i][j][1] = 1;
							}
						}
					}
				}
				else if (timeslot == "twentyfourhrseveryday")
				{
					for (int i = 0; i < 8; i++)
					{
						for (int j = 0; j < 24; j++)
						{
							timeRestrictions[i][j][0] = true;
							timeRestrictions[i][j][1] = true;

							timeRestrictions2[i][j][0] = 1;
							timeRestrictions2[i][j][1] = 1;
						}
					}
				}
				else if (timeslot == "twentyfourhrsweekdays")
				{
					for (int i = 1; i < 6; i++)
					{
						for (int j = 0; j < 24; j++)
						{
							if (i >= 1 && i <= 5)
							{
								timeRestrictions[i][j][0] = true;
								timeRestrictions[i][j][1] = true;

								timeRestrictions2[i][j][0] = 1;
								timeRestrictions2[i][j][1] = 1;
							}
						}
					}
				}
			}

			DepartmentDto departmentDto = new()
			{
				TerminalIds = terminalIds,
				TimeRestrictions2 = timeRestrictions2
			};

			List<DepartmentDto> departments = [departmentDto];

			WebSocketMessage webSocketMessage = new()
			{
				EventType = WebSocketEventType.UpdateQuickAccess,
				Data = [departmentDto],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
		}

		private async Task SetDepartmentAsync(string eventType, object[] data)
		{
			var id = Guid.NewGuid();
			string[] terminalIds = [];

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;

			var departmentId = jsonElement?.GetProperty("departmentId").GetString();
			var departmentName = jsonElement?.GetProperty("departmentName").GetString();
			var timeslot = jsonElement?.GetProperty("timeslot").GetString();

			if (jsonElement?.TryGetProperty("terminals", out var tempElement) == true)
			{
				if (tempElement.ValueKind == JsonValueKind.Array)
				{
					terminalIds = tempElement.EnumerateArray().Select(x => x.GetString()).ToArray();
				}
			}

			if (string.IsNullOrEmpty(departmentId))
			{
				await SendAsync(JsonSerializer.Serialize(new { eventType = "Error", data = "Invalid Input!" }));
				return;
			}

			bool[][][] timeRestrictions = null;
			int[][][] timeRestrictions2 = null;

			if (!string.IsNullOrEmpty(timeslot))
			{
				timeRestrictions = new bool[8][][];
				timeRestrictions2 = new int[8][][];

				for (int i = 0; i < 8; i++)
				{
					timeRestrictions[i] = new bool[24][];
					timeRestrictions2[i] = new int[24][];

					for (int j = 0; j < 24; j++)
					{
						timeRestrictions[i][j] = new bool[2];
						timeRestrictions2[i][j] = new int[2];
					}
				}

				if (timeslot == "none")
				{
				}
				else if (timeslot == "ninetofiveeveryday")
				{
					for (int i = 0; i < 8; i++)
					{
						for (int j = 9; j < 18; j++)
						{
							timeRestrictions[i][j][0] = true;
							timeRestrictions[i][j][1] = true;

							timeRestrictions2[i][j][0] = 1;
							timeRestrictions2[i][j][1] = 1;
						}
					}
				}
				else if (timeslot == "ninetofiveweekdays")
				{
					for (int i = 1; i < 6; i++)
					{
						for (int j = 9; j < 18; j++)
						{
							if (i >= 1 && i <= 5)
							{
								timeRestrictions[i][j][0] = true;
								timeRestrictions[i][j][1] = true;

								timeRestrictions2[i][j][0] = 1;
								timeRestrictions2[i][j][1] = 1;
							}
						}
					}
				}
				else if (timeslot == "twentyfourhrseveryday")
				{
					for (int i = 0; i < 8; i++)
					{
						for (int j = 0; j < 24; j++)
						{
							timeRestrictions[i][j][0] = true;
							timeRestrictions[i][j][1] = true;

							timeRestrictions2[i][j][0] = 1;
							timeRestrictions2[i][j][1] = 1;
						}
					}
				}
				else if (timeslot == "twentyfourhrsweekdays")
				{
					for (int i = 1; i < 6; i++)
					{
						for (int j = 0; j < 24; j++)
						{
							if (i >= 1 && i <= 5)
							{
								timeRestrictions[i][j][0] = true;
								timeRestrictions[i][j][1] = true;

								timeRestrictions2[i][j][0] = 1;
								timeRestrictions2[i][j][1] = 1;
							}
						}
					}
				}
			}

			DepartmentDto departmentDto = new()
			{
				DeptId = departmentId,
				DeptName = departmentName,
				TerminalIds = terminalIds,
				TimeRestrictions2 = timeRestrictions2
			};

			List<DepartmentDto> departments = [departmentDto];

			WebSocketMessage webSocketMessage = new()
			{
				EventType = eventType,
				Data = [departments],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
		}

		private async Task GetEmployeeAsync(object[] data)
		{
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var employeeId = jsonElement?.GetProperty("employeeId").GetString();
			var smartCardSNString = jsonElement?.GetProperty("smartCardSN").GetString();

			long smartCardSN = 0;

			if (!string.IsNullOrEmpty(smartCardSNString))
			{
				_ = long.TryParse(smartCardSNString, out smartCardSN);
			}

			var obj = new Dictionary<string, object>();

			if (!string.IsNullOrEmpty(employeeId))
			{
				obj.Add("employeeId", employeeId);
			}

			if (smartCardSN != 0)
			{
				obj.Add("smartCardSN", smartCardSN);
			}

			WebSocketMessage webSocketMessage = new()
			{
				EventType = WebSocketEventType.GetEmployee,
				Data = [obj],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);

			using var semaphoreSlim = new SemaphoreSlim(0, 1);
			memoryCache.Set(id, semaphoreSlim, DateTimeOffset.Now.AddSeconds(60));

			await clientWebSocketHandler?.SendAsync(jsonStr);

			// the following will fill the inputs in the employee form (to demo how handshake works for sean) (240321)
			using CancellationTokenSource cancellationTokenSource = new();
			Task task = semaphoreSlim.WaitAsync(cancellationTokenSource.Token);

			if (await Task.WhenAny(task, Task.Delay(10000, cancellationTokenSource.Token)) == task)
			{
				var result = memoryCache.Get(id);

				if (result is object[] v && v.Length > 0 && v[0] is JsonElement v0 && v0.ValueKind == JsonValueKind.String && v0.GetString() == "OK")
				{
					EmployeeDto employeeDto = JsonSerializer.Deserialize<EmployeeDto>(v[1].ToString());

					var cardSN = employeeDto.SmartCardSN == 0 ? "" : employeeDto.SmartCardSN.ToString();

					string[] myData = [employeeDto.LastName, employeeDto.FirstName, cardSN, employeeDto.IsActive.ToString().ToLower()];

					await SendAsync(JsonSerializer.Serialize(new { eventType = "onGetEmployee", data = myData }));
				}
			}

			cancellationTokenSource.Cancel();
		}

		private async Task DefaultEventTypeAsync(string eventType, object[] data)
		{
			WebSocketMessage webSocketMessage = new()
			{
				EventType = eventType,
				Data = data,
				Id = Guid.NewGuid(),
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task RequestInsertPermissionAsync(object[] data)
		{
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var employeeId = jsonElement?.GetProperty("employeeId").GetString();
			// var smartCardSNString = jsonElement?.GetProperty("smartCardSN").GetString();

			WebSocketMessage webSocketMessage = new()
			{
				EventType = WebSocketEventType.RequestInsertPermission,
				Data = [new { employeeId }],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task SetEmployeeAsync(string eventType, object[] data)
		{
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;

			var employeeId = jsonElement?.GetProperty("employeeId").GetString();
			var originalId = jsonElement?.GetProperty("originalId").GetString();
			var smartCardSN = jsonElement?.GetProperty("smartCardSN").GetString();
			var lastName = jsonElement?.GetProperty("lastName").GetString();
			var firstName = jsonElement?.GetProperty("firstName").GetString();
			var isActive = jsonElement?.GetProperty("isActive").GetString();
			var password = jsonElement?.GetProperty("password").GetString();

			if (string.IsNullOrEmpty(employeeId))
			{
				await SendAsync(JsonSerializer.Serialize(new { eventType = "Error", data = "Invalid Input!" }));
				return;
			}

			EmployeeDto employeeDto = new()
			{
				EmployeeId = employeeId,
				OriginalId = string.IsNullOrEmpty(originalId) ? null : originalId,
				SmartCardSN = string.IsNullOrEmpty(smartCardSN) ? 0 : long.Parse(smartCardSN),
				LastName = lastName,
				FirstName = firstName,
				Departments = ["TEST01", "TEST02", "TEST03", "EVERYONE"],
				IsActive = (isActive == "true"),
				Password = password,
				IsPassword = !string.IsNullOrEmpty(password),
				Email = "briandreleung@lucky.com.hk"
			};

			List<EmployeeDto> employees = [employeeDto];

			WebSocketMessage webSocketMessage = new()
			{
				EventType = eventType,
				Data = [employees],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
		}

		private async Task GetAccessRight(object[] data)
		{
			var id = Guid.NewGuid();
			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var smartCardSN = (jsonElement?.GetProperty("smartCardSN").GetInt64()) ?? 0;

			// convert to ISO 8601 (210125)
			var dateTimeISO8601 = DateTimeOffset.Now.ToString("s");

			// add ms at the end to follow marcus' whatsapp sample (210125)
			var random = new Random();
			dateTimeISO8601 += "." + random.Next(1000);

			WebSocketMessage webSocketMessage = new()
			{
				EventType = WebSocketEventType.GetAccessRight,
				Data = [new { smartCardSN, dateTime = dateTimeISO8601 }],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task AccessLog(object[] data)
		{
			var random = new Random();
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var cardSN = jsonElement?.GetProperty("cardSN").GetString();
			var status = jsonElement?.GetProperty("status").GetString();

			TerminalSettingsDto terminalSettingsDto;
			terminalSettingsDto = await terminalSettingsRepository.GetTerminalSettingsBySnAsync(sn);

			AccessLogDto accesslog = new()
			{
				Status = status,
				LogTime = DateTime.Now,
				TerminalID = terminalSettingsDto.TerminalId,
				JobCode = 0,
				BodyTemperature = Math.Round(((decimal)random.Next(366, 388)) / 10, 1),
				SmartCardSN = ulong.Parse(cardSN),
				Thumbnail = null,
				ByWhat = "S"
			};

			List<AccessLogDto> accessLogs = [accesslog];

			WebSocketMessage webSocketMessage = new()
			{
				EventType = WebSocketEventType.Accesslogs,
				Data = [accessLogs],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize(webSocketMessage, jsonSerializerOptionsIgnoreNull);

			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task AccessLogs(object[] data)
		{
			var random = new Random();
			var id = Guid.NewGuid();
			List<AccessLogDto> accessLogs = [];

			foreach (var item in data)
			{
				var jsonElement = JsonSerializer.Deserialize<JsonElement>(item.ToString()) as JsonElement?;
				var cardSN = jsonElement?.GetProperty("cardSN").GetString();
				var status = jsonElement?.GetProperty("status").GetString();

				TerminalSettingsDto terminalSettingsDto = await terminalSettingsRepository.GetTerminalSettingsBySnAsync(sn);

				accessLogs.Add(new AccessLogDto
				{
					Status = status,
					LogTime = DateTime.Now,
					TerminalID = terminalSettingsDto.TerminalId,
					JobCode = 0,
					BodyTemperature = Math.Round(((decimal)random.Next(366, 388)) / 10, 1),
					SmartCardSN = ulong.Parse(cardSN),
					Thumbnail = null,
					ByWhat = "S"
				});
			}

			WebSocketMessage webSocketMessage = new()
			{
				EventType = WebSocketEventType.Accesslogs,
				Data = [accessLogs],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}
	}
}
