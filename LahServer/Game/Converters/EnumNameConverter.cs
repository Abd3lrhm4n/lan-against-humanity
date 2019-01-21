using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game.Converters
{
	class EnumNameConverter : JsonConverter
	{
		private static Dictionary<Type, Dictionary<string, object>> _nameToEnumMap;
		private static Dictionary<Type, Dictionary<object, string>> _enumToNameMap;

		static EnumNameConverter()
		{
			_nameToEnumMap = new Dictionary<Type, Dictionary<string, object>>();
			_enumToNameMap = new Dictionary<Type, Dictionary<object, string>>();
		}

		private static void RegisterEnum(Type enumType)
		{
			if (_nameToEnumMap.ContainsKey(enumType)) return;

			var nameToEnum = _nameToEnumMap[enumType] = new Dictionary<string, object>();
			var enumToName = _enumToNameMap[enumType] = new Dictionary<object, string>();

			var pairs = enumType.GetFields()
				.Where(f => f.FieldType == f.DeclaringType)
				.Select(f => (val: Enum.ToObject(enumType, f.GetRawConstantValue()), name: f.GetCustomAttribute<NameAttribute>()?.Name))
				.Where(pair => !string.IsNullOrWhiteSpace(pair.name));

			foreach (var pair in pairs)
			{
				nameToEnum[pair.name] = pair.val;
				enumToName[pair.val] = pair.name;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType.IsEnum;
		}

		public override bool CanWrite => true;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var defaultValue = Activator.CreateInstance(objectType);
			RegisterEnum(objectType);
			var token = JToken.Load(reader);
			if (token == null) return defaultValue;
			var strValue = token.Value<string>();
			if (!_nameToEnumMap.TryGetValue(objectType, out var map)) return defaultValue;
			if (!map.TryGetValue(strValue, out var val)) return defaultValue;
			return val;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			var type = value.GetType();
			RegisterEnum(type);
			if (_enumToNameMap.TryGetValue(type, out var map) && map.TryGetValue(value, out var str))
			{
				writer.WriteValue(str);
			}
			else
			{
				writer.WriteNull();
			}
		}
	}
}
