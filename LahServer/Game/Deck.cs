using LahServer.Game.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class Deck
    {
        private const string DefaultName = "Untitled Deck";

        private readonly Dictionary<string, BlackCard> _blackCards;
        private readonly Dictionary<string, WhiteCard> _whiteCards;

        [JsonProperty("cards")]        
        private readonly List<Card> _cards;

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(DefaultName)]
        public string Name { get; private set; } = DefaultName;

        public Deck()
        {
            _blackCards = new Dictionary<string, BlackCard>();
            _whiteCards = new Dictionary<string, WhiteCard>();
            _cards = new List<Card>();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            foreach(var card in _cards)
            {
                card.Owner = this;

                if (card is WhiteCard whiteCard)
                {
                    _whiteCards[card.ID] = whiteCard;
                }
                else if (card is BlackCard blackCard)
                {
                    _blackCards[card.ID] = blackCard;
                }
            }
        }

        public WhiteCard GetWhiteCard(string id) => _whiteCards.TryGetValue(id, out var card) ? card : null;

        public BlackCard GetBlackCard(string id) => _blackCards.TryGetValue(id, out var card) ? card : null;

        public IEnumerable<WhiteCard> GetWhiteCards() => _whiteCards.Values;

        public IEnumerable<BlackCard> GetBlackCards() => _blackCards.Values;

        public IEnumerable<Card> GetAllCards() => _cards.AsEnumerable();

        public override string ToString() => $"{Name} ({_cards.Count})";
    }
}
