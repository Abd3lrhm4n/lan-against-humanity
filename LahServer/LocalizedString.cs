using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	[JsonConverter(typeof(Converter))]
	public sealed class LocalizedString
	{
		private const string DefaultLocale = "en-US";

		private readonly Dictionary<string, string> _stringValues;

		public LocalizedString()
		{
			_stringValues = new Dictionary<string, string>();
		}

		public string this[string langCode]
		{
			get
			{
				if (String.IsNullOrWhiteSpace(langCode)) throw new ArgumentException("The provided language code is blank.");

				var parts = langCode.SplitTrim(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

				for (int i = parts.Length; i > 0; i--)
				{
					if (_stringValues.TryGetValue(parts.LimitedConcat(i, "-"), out var value)) return value;
				}

				return _stringValues.TryGetValue(DefaultLocale, out var val) ? val : _stringValues.Values.FirstOrDefault() ?? string.Empty;
			}

			set
			{
				if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException("The provided language code is blank.");

				_stringValues[langCode] = value.SplitTrim(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries).LimitedConcat(-1, "-");
			}
		}

		private sealed class Converter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(LocalizedString);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				var o = JToken.ReadFrom(reader) as JObject;
				if (o == null) throw new JsonReaderException($"No object found for reading {nameof(LocalizedString)}.");
				var ls = new LocalizedString();
				foreach (var kvp in o)
				{
					var str = kvp.Value.Value<string>();
					if (str == null) continue;
					ls[kvp.Key] = str;
				}
				return ls;
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				serializer.Serialize(writer, (value as LocalizedString)?._stringValues);
			}
		}
	}
}
