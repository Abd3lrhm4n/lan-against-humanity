using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LahServer.Game
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class Card
    {
        [JsonProperty("content")]
        private readonly Dictionary<string, string> _content = new Dictionary<string, string>();

        [JsonProperty("id")]
        public string ID { get; internal set; }

        public Deck Owner { get; internal set; }

		public bool IsCustom { get; private set; }

		public void AddContent(string languageCode, string content) => _content[languageCode] = content;

        public string GetContent(string languageCode) => String.IsNullOrWhiteSpace(languageCode) || !_content.TryGetValue(languageCode, out var c) ? null : c;

		public static WhiteCard CreateCustom(string content)
		{
			if (String.IsNullOrWhiteSpace(content)) return null;
			var card = new WhiteCard
			{
				ID = $"custom: {content}",
				IsCustom = true
			};
			card.AddContent("en-US", content);
			return card;
		}
    }
}
