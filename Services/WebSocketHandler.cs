using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
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
		protected string iGuardPayrollIpPort;
		protected string regCode;

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
			var receiveBuffer = new ArraySegment<Byte>(new byte[100]);

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
							iGuardPayrollIpPort = jsonElement?.GetProperty("IpPort").GetString();
							regCode = jsonElement?.GetProperty("RegCode").GetString();

							//if (Terminal.SN != sn)
							//{
							//    Terminal.SN = sn;
							//    SaveTerminal();
							//}

							// connect to iGuardPayroll (201201)
							clientWebSocketHandler = new ClientWebSocketHandler(this, sn, iGuardPayrollIpPort, regCode, memoryCache);
							_ = clientWebSocketHandler.Initialize();
						}
						else if (eventType == "accessLog")
						{
							await AccessLog(jsonObj.Data);
						}
						else if (eventType == "accessLogs")
						{
							await AccessLogs(jsonObj.Data);
						}
						else if (eventType == "GetAccessRight")
						{
							await GetAccessRight(jsonObj.Data);
						}
						else if (eventType == "RequestPermission")
						{
							await RequestInsertPermissionAsync(jsonObj.Data);
						}
						else if (eventType == "GetEmployee")
						{
							_ = GetEmployeeAsync(jsonObj.Data);
						}
						else if (eventType == "AddEmployee")
						{
							await AddEmployeeAsync(jsonObj.Data);
						}
						else if (eventType == "UpdateEmployee")
						{
							await UpdateEmployeeAsync(jsonObj.Data);
						}
						else if (eventType == "DeleteEmployee")
						{
							await DeleteEmployeeAsync(jsonObj.Data);
						}
						else if (eventType == "GetDepartment")
						{
							_ = GetDepartmentAsync(jsonObj.Data);
						}
						else if (eventType == "AddDepartment")
						{
							await AddDepartmentAsync(jsonObj.Data);
						}
						else if (eventType == "UpdateDepartment")
						{
							await UpdateDepartmentAsync(jsonObj.Data);
						}
						else if (eventType == "DeleteDepartment")
						{
							await DeleteDepartmentAsync(jsonObj.Data);
						}
						else
						{
							if (clientWebSocketHandler != null)
							{
								await UnknownEventTypeAsync(eventType, jsonObj.Data);
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

		private async Task DeleteDepartmentAsync(object[] data)
		{
			await GetOrDeleteDepartmentAsync("DeleteDepartment", data);
		}

		private async Task GetDepartmentAsync(object[] data)
		{
			await GetOrDeleteDepartmentAsync("GetDepartment", data);
		}

		private async Task GetOrDeleteDepartmentAsync(string eventType, object[] data)
		{
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var departmentId = jsonElement?.GetProperty("departmentId").GetString();

			Dictionary<string, object> obj = null;

			if (!string.IsNullOrEmpty(departmentId))
			{
				obj = new Dictionary<string, object> { { "departmentId", departmentId } };
			}

			WebSocketMessage webSocketMessage = new()
			{
				EventType = eventType,
				Data = obj is null ? [] : [obj],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task UpdateDepartmentAsync(object[] data)
		{
			await SetDepartmentAsync(WebSocketEventType.UpdateDepartment, data);
		}

		private async Task AddDepartmentAsync(object[] data)
		{
			await SetDepartmentAsync(WebSocketEventType.AddDepartment, data);
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

			if (!string.IsNullOrEmpty(timeslot))
			{
				timeRestrictions = new bool[8][][];
				for (int i = 0; i < 8; i++)
				{
					timeRestrictions[i] = new bool[24][];
					for (int j = 0; j < 24; j++) { timeRestrictions[i][j] = new bool[2]; }
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
				TimeRestrictions = timeRestrictions
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
				EventType = "GetEmployee",
				Data = [obj],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);

			using var semaphoreSlim = new SemaphoreSlim(0, 1);
			memoryCache.Set(id, semaphoreSlim, DateTimeOffset.Now.AddSeconds(60));

			await clientWebSocketHandler?.SendAsync(jsonStr);

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

		private async Task DeleteEmployeeAsync(object[] data)
		{
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var employeeId = jsonElement?.GetProperty("employeeId").GetString();

			WebSocketMessage webSocketMessage = new()
			{
				EventType = "DeleteEmployee",
				Data = [employeeId],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task UnknownEventTypeAsync(string eventType, object data)
		{
			var id = Guid.NewGuid();

			WebSocketMessage webSocketMessage = new()
			{
				EventType = eventType,
				Data = [data],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task RequestInsertPermissionAsync(object[] data)
		{
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var employeeId = jsonElement?.GetProperty("employeeId").GetString();

			WebSocketMessage webSocketMessage = new()
			{
				EventType = "RequestInsertPermission",
				Data = [new { employeeId }],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}

		private async Task AddEmployeeAsync(object[] data)
		{
			await SetEmployeeAsync("AddEmployees", data);
		}

		private async Task UpdateEmployeeAsync(object[] data)
		{
			await SetEmployeeAsync("UpdateEmployees", data);
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

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "GetAccessRight",
				Data = [new { smartCardSN, dateTime = dateTimeISO8601 }],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
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

			List<AccessLogDto> accessLogs = new() { accesslog };

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "Accesslogs",
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
				EventType = "Accesslogs",
				Data = [accessLogs],
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, jsonSerializerOptionsIgnoreNull);
			await clientWebSocketHandler?.SendAsync(jsonStr);
		}
	}
}
