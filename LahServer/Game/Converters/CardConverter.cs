using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace LahServer.Game.Converters
{
    class CardConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Card);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {   
            var o = JToken.ReadFrom(reader) as JObject;
            if (o == null) throw new ArgumentException("No object found at current reader position.");
            var id = o["id"]?.Value<string>();
            if (id == null) throw new ArgumentException("Object is missing 'id' property.");

            if (id.StartsWith("w_"))
            {
                return o.ToObject<WhiteCard>();
            }
            else if (id.StartsWith("b_"))
            {
                return o.ToObject<BlackCard>();
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
