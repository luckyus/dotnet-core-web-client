using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client
{
	public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
	{
		// ref: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0 (210125)
		private const string ISO8601 = "yyyy-MM-ddTHH:mm:ss.fff";

		public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return DateTimeOffset.Parse(reader.GetString(), CultureInfo.InvariantCulture);
		}

		public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString(ISO8601, CultureInfo.InvariantCulture));
		}
	}
}
