﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class WebSocketMessage
	{
		[JsonPropertyName("eventType")]
		public string EventType { get; set; }
		[JsonPropertyName("data")]
		public object[] Data { get; set; }
		[JsonPropertyName("timeStamp")]
		public long? TimeStamp { get; set; } = null;    // include this to avoid sending SetTimeSTamp seperately (230811)
		[JsonPropertyName("id")]
		public Guid? Id { get; set; } = null;
		[JsonPropertyName("ackId")]
		public Guid? AckId { get; set; } = null;
	}
}
