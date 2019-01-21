using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class LahSettings
	{
		private const int DefaultHandSize = 10;
		private const int DefaultMinPlayers = 3;
		private const int DefaultMaxPlayers = 10;
		private const int MinMaxPlayers = 3;
		private const int MinMinPlayers = 3;
		private const int MinMaxPoints = 1;
		private const int MinRoundEndTimeout = 0;
		private const int MinGameEndTimeout = 10000;
		private const int MinAfkTimeSeconds = 30;
		private const int MinAfkRecoveryTimeSeconds = 30;
		private const int MinBlankCards = 0;

		private const int DefaultRoundEndTimeout = 10000;
		private const int DefaultGameEndTimeout = 30000;
		private const int DefaultAfkTimeSeconds = 300;
		private const int DefaultAfkRecoveryTimeSeconds = 90;
		private const int DefaultMaxPoints = 10;
		private const int DefaultBlankCards = 0;

		private int _blankCards = DefaultBlankCards;
		private int _maxPoints = DefaultMaxPoints;
		private int _maxPlayers = DefaultMaxPlayers, _maxNameLength;
		private int _handSize = DefaultHandSize;
		private int _minPlayers = DefaultMinPlayers;
		private int _roundEndTimeout = DefaultRoundEndTimeout;
		private int _gameEndTimeout = DefaultGameEndTimeout;

		private int _afkTimeSeconds = DefaultAfkTimeSeconds;
		private int _afkRecoveryTimeSeconds = DefaultAfkRecoveryTimeSeconds;

		[JsonProperty("host", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue("http://localhost:80")]
		public string Host { get; set; }

		[JsonProperty("min_players", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultMinPlayers)]
		public int MinPlayers
		{
			get => _minPlayers;
			set => _minPlayers = value < MinMinPlayers ? MinMinPlayers : value;
		}

		[JsonProperty("max_players", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultMaxPlayers)]
		public int MaxPlayers
		{
			get => _maxPlayers;
			set => _maxPlayers = value < MinMaxPlayers ? MinMaxPlayers : value;
		}

		[JsonProperty("max_player_name_length", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(48)]
		public int MaxPlayerNameLength
		{
			get => _maxNameLength;
			set => _maxNameLength = value <= 0 ? 1 : value;
		}

		[JsonProperty("hand_size", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(10)]
		public int HandSize
		{
			get => _handSize;
			set => _handSize = value <= 4 ? 4 : value;
		}

		[JsonProperty("blank_cards", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultBlankCards)]
		public int BlankCards
		{
			get => _blankCards;
			set => _blankCards = value <= MinBlankCards ? MinBlankCards : value;
		}

		[JsonProperty("round_end_timeout", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultRoundEndTimeout)]
		public int RoundEndTimeout
		{
			get => _roundEndTimeout;
			set => _roundEndTimeout = value < MinRoundEndTimeout ? MinRoundEndTimeout : value;
		}

		[JsonProperty("game_end_timeout", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultGameEndTimeout)]
		public int GameEndTimeout
		{
			get => _gameEndTimeout;
			set => _gameEndTimeout = value < MinGameEndTimeout ? MinGameEndTimeout : value;			
		}

		[JsonProperty("afk_time_seconds", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultAfkTimeSeconds)]
		public int AfkTimeSeconds
		{
			get => _afkTimeSeconds;
			set => _afkTimeSeconds = value < MinAfkTimeSeconds ? MinAfkTimeSeconds : value;
		}

		[JsonProperty("afk_recovery_time_seconds", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultAfkRecoveryTimeSeconds)]
		public int AfkRecoveryTimeSeconds
		{
			get => _afkRecoveryTimeSeconds;
			set => _afkRecoveryTimeSeconds = value < MinAfkRecoveryTimeSeconds ? MinAfkRecoveryTimeSeconds : value;
		}

		[JsonProperty("perma_czar", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(false)]
		public bool PermanentCzar { get; set; }

		[JsonProperty("max_points", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(DefaultMaxPoints)]
		public int MaxPoints
		{
			get => _maxPoints;
			set
			{
				_maxPoints = value < MinMaxPoints ? MinMaxPoints : value;
			}
		}

		public static LahSettings FromFile(string path) => JsonConvert.DeserializeObject<LahSettings>(File.ReadAllText(path));
	}
}
