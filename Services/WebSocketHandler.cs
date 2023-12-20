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
using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using dotnet_core_web_client.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_core_web_client.Services
{
	public class WebSocketHandler : IWebSocketHandler
	{
		protected WebSocket webSocket;
		protected ClientWebSocketHandler clientWebSocketHandler = null;
		readonly string smartCardSNConfigPath = Directory.GetCurrentDirectory() + "/DBase/smartCardSN.json";

		// to be assigned in onConnectClick eventType (230709)
		protected string sn;
		protected string iGuardPayrollIpPort;
		protected string regCode;

		// private readonly IServiceScopeFactory _scopeFactory;
		public ITerminalSettingsRepository _terminalSettingsRepository;
		public ITerminalRepository _terminalRepository;
		public INetworkRepository _networkRepository;

		protected JsonSerializerOptions jsonSerializerOptionsIgnoreNull = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

		public WebSocketHandler(ITerminalSettingsRepository terminalSettingsRepository, ITerminalRepository terminalRepository, INetworkRepository networkRepository)
		{
			//_scopeFactory = serviceScopeFactory;
			_terminalSettingsRepository = terminalSettingsRepository;
			_terminalRepository = terminalRepository;
			_networkRepository = networkRepository;
		}

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
							object[] data = { "iGuardPayroll Connecting...", smartCardSNArray };
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
							clientWebSocketHandler = new ClientWebSocketHandler(this, sn, iGuardPayrollIpPort, regCode);
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
						else if (eventType == "AddEmployee")
						{
							await AddEmployeeAsync(jsonObj.Data);
						}
						else if (eventType == "RequestPermission")
						{
							await RequestInsertPermissionAsync(jsonObj.Data);
						}
						else if (eventType == "DeleteEmployee")
						{
							await DeleteEmployeeAsync(jsonObj.Data);
						}
						else
						{
							if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
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
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;

			var employeeId = jsonElement?.GetProperty("employeeId").GetString();
			var lastName = jsonElement?.GetProperty("lastName").GetString();
			var firstName = jsonElement?.GetProperty("firstName").GetString();
			var isActive = jsonElement?.GetProperty("isActive").GetString();

			EmployeeDto employeeDto = new()
			{
				EmployeeId = employeeId,
				LastName = lastName,
				FirstName = firstName,
				Departments = ["TEST01", "TEST02", "TEST03", "EVERYONE"],
				IsActive = (isActive == "true"),
			};

			List<EmployeeDto> employees = [employeeDto];

			WebSocketMessage webSocketMessage = new()
			{
				EventType = "Employee",
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
			terminalSettingsDto = await _terminalSettingsRepository.GetTerminalSettingsBySnAsync(sn);

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

				TerminalSettingsDto terminalSettingsDto = await _terminalSettingsRepository.GetTerminalSettingsBySnAsync(sn);

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
