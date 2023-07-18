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
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_core_web_client.Services
{
	public class WebSocketHandler : IWebSocketHandler
	{
		protected WebSocket webSocket;
		protected ClientWebSocketHandler clientWebSocketHandler = null;
		readonly string terminalConfigPath = Directory.GetCurrentDirectory() + "/DBase/terminal.json";
		readonly string terminalSettingsConfigPath = Directory.GetCurrentDirectory() + "/DBase/terminalSettings.json";
		readonly string networkConfigPath = Directory.GetCurrentDirectory() + "/DBase/network.json";
		readonly string smartCardSNConfigPath = Directory.GetCurrentDirectory() + "/DBase/smartCardSN.json";

		// to be assigned in onConnectClick eventType (230709)
		protected string sn;
		protected string iGuardPayrollIpPort;
		protected string regCode;

		private readonly IServiceScopeFactory _scopeFactory;
		private ITerminalSettingsRepository _terminalSettingsRepository;
		private ITerminalRepository _terminalRepository;

		public WebSocketHandler(ITerminalSettingsRepository terminalSettingsRepository, ITerminalRepository terminalRepository)
		{
			_terminalSettingsRepository = terminalSettingsRepository;
			_terminalRepository = terminalRepository;
		}

		private TerminalSettingsDto _TerminalSettingsDto = null;
		public TerminalSettingsDto TerminalSettingsDto
		{
			get
			{
				if (_TerminalSettingsDto == null)
				{
					_TerminalSettingsDto = _terminalSettingsRepository.GetTerminalSettingsBySnAsync(sn).Result;

					if (_TerminalSettingsDto == null)
					{
						_TerminalSettingsDto = new TerminalSettingsDto
						{
							TerminalId = "DOTNET",
							Description = "My iGuardExpress 540 Machine",
							Language = "en-us",
							DateTimeFormat = "dd/mm/yy",
							AllowedOrigins = new string[] { "http://iguardexpress.azurewebsites.net", "http://localhost:3000" },
							CameraControl = new CameraControlDto
							{
								Enable = true,
								Resolution = CameraResolution.r640x480,
								FrameRate = 1,
								Environment = CameraEnvironment.Normal
							},
							SmartCardControl = new SmartCardControlDto
							{
								IsReadCardSNOnly = true,
								AcceptUnknownCard = false,
								CardType = SmartCardType.MifareOnly,
								AcceptUnregisteredCard = false
							},
							InOutControl = new InOutControlDto
							{
								DefaultInOut = InOutStrategy.SystemInOut,
								IsEnableFx = new bool[] { true, false, true, false },
								DailyResetAutoInOut = true,
								DailyResetAutoInOutTime = "00:00",
							},
							InOutTigger = new SortedDictionary<string, InOutStatus>
								{
									{ "00:00", InOutStatus.IN },
									{ "12:00", InOutStatus.OUT },
									{ "13:00", InOutStatus.IN },
									{ "23:59", InOutStatus.OUT }
								},
							LocalDoorRelayControl = new LocalDoorRelayControlDto
							{
								DoorRelayStatus = new DoorRelayStatus() { In = true },
								Duration = 3000
							},
							RemoteDoorRelayControl = new RemoteDoorRelayControlDto
							{
								Enabled = true,
								Id = 123,
								DelayTimer = 3000,
								AccessRight = AccessRight.System
							},
							DailyReboot = new DailyRebootDto
							{
								Enabled = true,
								Time = "02:00"
							},
							TimeSync = new TimeSyncDto
							{
								TimeZone = "Asia/Hong_Kong",
								TimeOffSet = 8,
								TimeServer = "time.google.com",
								IsEnableSNTP = true,
								IsSyncMasterTime = true
							},
							AntiPassback = new AntiPassbackDto
							{
								Type = "System",
								IsDailyReset = true,
								DailyResetTime = "02:00"
							},
							DailySingleAccess = new DailySingleAccessDto
							{
								Type = "System",
								IsDailyReset = true,
								DailyResetTime = "02:00"
							},
							TempDetectEnable = false,
							FaceDetectEnable = false,
							FlashLightEnabled = false,
							TempCacheDuration = 3000,
							AutoUpdateEnabled = false,
						};

						_ = _terminalSettingsRepository.UpsertTerminalSettingsAsync(_TerminalSettingsDto, sn);
					}
				}

				return _TerminalSettingsDto;
			}
			set
			{
				_ = Task.Run(async () =>
				{
					if (await _terminalSettingsRepository.UpsertTerminalSettingsAsync(value, sn) != null)
					{
						_TerminalSettingsDto = value;
					}
				});
			}
		}

		private TerminalsDto _TerminalDto = null;
		public TerminalsDto TerminalDto
		{
			get
			{
				if (_TerminalDto == null)
				{
					_TerminalDto = _terminalRepository.GetTerminalsBySnAsync(sn).Result;

					if (_TerminalDto == null)
					{
						_TerminalDto = new() { SN = sn };
						_ = _terminalRepository.UpsertTerminalsAsync(_TerminalDto);
					}
				}
				return _TerminalDto;
			}
			set
			{
				_ = Task.Run(async () =>
				{
					if (await _terminalRepository.UpsertTerminalsAsync(value) != null)
					{
						_TerminalDto = value;
					}
				});
			}
		}

		private Network _Network = null;
		public Network Network
		{
			get
			{
				if (_Network == null)
				{
					if (File.Exists(networkConfigPath))
					{
						string jsonStr = File.ReadAllText(networkConfigPath);
						_Network = JsonSerializer.Deserialize<Network>(jsonStr);
					}
					else
					{
						_Network = new Network();
						string jsonStr = JsonSerializer.Serialize<Network>(_Network, new JsonSerializerOptions { IgnoreNullValues = true });
						File.WriteAllText(networkConfigPath, jsonStr);
					}
				}
				return _Network;
			}
			set
			{
				string jsonStr = JsonSerializer.Serialize<Network>(value, new JsonSerializerOptions { IgnoreNullValues = true });
				File.WriteAllText(networkConfigPath, jsonStr);
				_Network = value;
			}
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
				Data = new Object[] { new { smartCardSN, dateTime = dateTimeISO8601 } },
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
		}

		private async Task AccessLog(object[] data)
		{
			var random = new Random();
			var id = Guid.NewGuid();

			var jsonElement = JsonSerializer.Deserialize<JsonElement>(data[0].ToString()) as JsonElement?;
			var cardSN = jsonElement?.GetProperty("cardSN").GetString();
			var status = jsonElement?.GetProperty("status").GetString();

			AccessLog accesslog = new AccessLog
			{
				Status = status,
				LogTime = DateTime.Now,
				TerminalID = TerminalSettingsDto.TerminalId,
				JobCode = 0,
				BodyTemperature = Math.Round(((decimal)random.Next(366, 388)) / 10, 1),
				SmartCardSN = ulong.Parse(cardSN),
				Thumbnail = null,
				ByWhat = "S"
			};

			List<AccessLog> accessLogs = new List<AccessLog> { accesslog };

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "Accesslogs",
				Data = new Object[] { accessLogs },
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
		}

		private async Task AccessLogs(object[] data)
		{
			var random = new Random();
			var id = Guid.NewGuid();
			List<AccessLog> accessLogs = new List<AccessLog>();

			foreach (var item in data)
			{
				var jsonElement = JsonSerializer.Deserialize<JsonElement>(item.ToString()) as JsonElement?;
				var cardSN = jsonElement?.GetProperty("cardSN").GetString();
				var status = jsonElement?.GetProperty("status").GetString();

				accessLogs.Add(new AccessLog
				{
					Status = status,
					LogTime = DateTime.Now,
					TerminalID = TerminalSettingsDto.TerminalId,
					JobCode = 0,
					BodyTemperature = Math.Round(((decimal)random.Next(366, 388)) / 10, 1),
					SmartCardSN = ulong.Parse(cardSN),
					Thumbnail = null,
					ByWhat = "S"
				});
			}

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "Accesslogs",
				Data = new Object[] { accessLogs },
				Id = id,
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			if (clientWebSocketHandler != null) await clientWebSocketHandler.SendAsync(jsonStr);
		}
	}
}
