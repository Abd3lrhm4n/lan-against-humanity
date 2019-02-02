using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LahServer.Game
{
	public delegate void PlayerJoinedEventDelegate(LahPlayer player);
	public delegate void PlayerLeftEventDelegate(LahPlayer player, string reason);
	public delegate void PlayersChangedEventDelegate();
	public delegate void GameStateChangedEventDelegate();
	public delegate void RoundStartedEventDelegate();
	public delegate void GameStageChangedEventDelegate(in GameStage oldStage, in GameStage currentStage);
	public delegate void RoundEndedEventDelegate(int round, LahPlayer roundWinner);
	public delegate void GameEndedEventDelegate(LahPlayer[] winners);

	// TODO: Find some way to combine state updates
	public sealed class LahGame
	{
		#region Constants
		private const string DefaultPlayerName = "Player";

		// Shuffle iterations
		private const int ShufflePasses = 100;

		private static readonly HashSet<char> PlayerNameCharExceptions = new HashSet<char>(new[] { ' ', '-', '_', '\'', '\"', '\u00ae', '\u2122', '.', ',' });
		#endregion

		// Index of the current black card being used
		private int _blackCardIndex;

		// ID for next player created
		private int _nextPlayerId = 0;

		// White cards drawn by players, also used for dealing
		private readonly HashList<WhiteCard> _whiteDrawPile;

		// White cards discarded from previous players
		private readonly HashList<WhiteCard> _whiteDiscardPile;

		// All black cards in the game
		private readonly HashList<BlackCard> _blackCards;

		// All white cards in the game
		private readonly HashList<WhiteCard> _whiteCards;

		// All cards in the game
		private readonly Dictionary<string, Card> _cards;

		// RNG
		private readonly Random _rng;

		// Current players in the game
		private readonly HashList<LahPlayer> _players;

		// Current stage of the game
		private GameStage _stage = GameStage.GameStarting;

		// Current round judge
		private int _judgeIndex = -1;

		// Cards played for round. Only populated once judging has begun
		private readonly HashList<(LahPlayer, WhiteCard[])> _roundPlays;

		// Index of winning play
		private int _winningPlayIndex = -1;

		// Round number
		private int _roundNum = 0;

		private readonly object _allPlayersSync = new object();
		private readonly object _stageChangeLock = new object();

		// Raised when player joins game
		public event PlayerJoinedEventDelegate PlayerJoined;
		// Raised when player leaves game
		public event PlayerLeftEventDelegate PlayerLeft;
		// Raised when game state is updated
		public event GameStateChangedEventDelegate GameStateChanged;
		// Raised when a player has joined, left, or been modified
		public event PlayersChangedEventDelegate PlayersChanged;
		// Raised when a new round has started
		public event RoundStartedEventDelegate RoundStarted;
		// Raised when game stage has changed
		public event GameStageChangedEventDelegate StageChanged;
		// Raised when a round has ended
		public event RoundEndedEventDelegate RoundEnded;

		public LahGame(IEnumerable<Pack> decks, LahSettings settings)
		{
			Settings = settings;
			_whiteDrawPile = new HashList<WhiteCard>();
			_whiteDiscardPile = new HashList<WhiteCard>();
			_blackCards = new HashList<BlackCard>();
			_whiteCards = new HashList<WhiteCard>();
			_players = new HashList<LahPlayer>();
			_rng = new Random();
			_cards = new Dictionary<string, Card>();
			_roundPlays = new HashList<(LahPlayer, WhiteCard[])>();

			// Combine decks and remove duplicates
			foreach (var card in decks.SelectMany(d => d.GetAllCards()))
			{
				if (!_cards.ContainsKey(card.ID))
				{
					_cards.Add(card.ID, card);
				}
				else
				{
					Console.WriteLine($"Duplicate card ID: {card.ID} in [{card.Owner.Name}]");
				}
			}

			_blackCards.AddRange(_cards.Values.OfType<BlackCard>());
			_whiteCards.AddRange(_cards.Values.OfType<WhiteCard>());

			ResetCards();
			NextJudge();
		}

		/// <summary>
		/// Resets piles and re-deals to players.
		/// </summary>
		private void ResetCards()
		{
			lock (_allPlayersSync)
			{
				var playerArray = _players.ToArray();

				// Take everyone's cards away
				foreach (var player in playerArray)
				{
					player.DiscardHand();
					player.DiscardSelection();
					player.ClearPreviousPlays();
				}

				// Reset the draw pile and shuffle it
				_whiteDrawPile.Clear();
				_whiteDrawPile.AddRange(_whiteCards);
				Shuffle(_whiteDrawPile);

				// Shuffle the black cards to keep it fresh
				Shuffle(_blackCards);

				// Reset the discard pile and black card index
				_whiteDiscardPile.Clear();
				_blackCardIndex = 0;

				// Deal cards out again
				foreach (var player in playerArray)
				{
					if (!_players.Contains(player)) continue;
					Deal(player);
				}
			}
		}

		private void NewRound()
		{
			lock (_allPlayersSync)
			{
				// Reset player selections
				foreach (var player in _players)
				{
					player.DiscardSelection();
				}

				// Move to next black card
				_blackCardIndex++;
				// Move to next judge
				NextJudge();
				// Move to next round
				_roundNum++;
				// Change stage
				Stage = GameStage.RoundInProgress;
				// Raise round started event
				RaiseRoundStarted();
			}
		}

		private void EndGame()
		{
			lock (_allPlayersSync)
			{
				Stage = GameStage.GameEnd;
				GameEndTimeoutAsync();
			}
		}

		private void NextJudge()
		{
			lock (_allPlayersSync)
			{
				var judge = Judge;
				int n = PlayerCount;
				var assholes = _players.Select((p, i) => (index: i, player: p)).Where(t => t.player.IsAsshole).ToArray();

				// If nobody's home, it's simple; nobody is the judge. Goodbye.
				if (n == 0)
				{
					_judgeIndex = -1;
					return;
				}

				if (Settings.PermanentCzar)
				{
					// If no judge or the judge is AFK, pick a new one
					if (_judgeIndex < 0 || (judge != null && judge.IsAfk))
					{
						// Start at a random point in the player list and linear search until a non-AFK player is found
						int offset = _rng.Next(n);
						for (int i = 0; i < n; i++)
						{
							int index = (i + offset) % n;
							var judgeCandidate = _players[index];
							// Ignore them if they're AFK
							if (judgeCandidate.IsAfk) continue;
							_judgeIndex = index;
							return;
						}
						// If everyone's AFK, just make a random person judge, oh well.
						_judgeIndex = _rng.Next(n);
					}
				}
				else
				{
					// Let's see if someone needs to be punished.
					if (assholes.Length > 0 && _rng.Next(0, 2) == 0)
					{
						int offset = _rng.Next(assholes.Length);
						for (int i = 0; i < assholes.Length; i++)
						{
							int index = (offset + i) % assholes.Length;
							// Ignore AFK assholes
							if (assholes[index].player.IsAfk) continue;
							_judgeIndex = assholes[index].index;
							return;
						}
					}

					// Make the next non-AFK person the judge.
					for (int i = 1; i < n; i++)
					{
						int index = (_judgeIndex + i) % n;
						var judgeCandidate = _players[index];
						if (judgeCandidate.IsAfk) continue;
						_judgeIndex = index;
					}

					// Fallback
					_judgeIndex = PlayerCount > 0 ? (_judgeIndex + 1) % n : -1;
				}
			}
		}

		private void NewGame()
		{
			ClearRoundPlays();
			ResetCards();
			_roundNum = 0;
			_judgeIndex = -1;
			Stage = GameStage.GameStarting;
		}

		private void Shuffle<T>(HashList<T> list)
		{
			int n = list.Count;
			for (int i = 0; i < ShufflePasses; i++)
			{
				for (int j = 0; j < n; j++)
				{
					int s = (_rng.Next(n - 1) + j + 1) % n;
					list.Swap(j, s);
				}
			}
		}

		/// <summary>
		/// Enumerates all players in the game.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<LahPlayer> GetPlayers()
		{
			lock (_allPlayersSync)
			{
				return _players.ToArray();
			}
		}

		/// <summary>
		/// Enumerates the current top player(s) in the game.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<LahPlayer> GetWinningPlayers()
		{
			lock (_allPlayersSync)
			{
				int maxScore = _players.Max(p => p.Score);
				foreach (var player in _players.Where(p => p.Score == maxScore))
				{
					yield return player;
				}
			}
		}

		/// <summary>
		/// Enumerates all players that still need to play cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<LahPlayer> GetPendingPlayers()
		{
			lock (_allPlayersSync)
			{
				if (Stage != GameStage.RoundInProgress) yield break;
				bool activeRound = false;
				foreach (var player in _players.ToArray())
				{
					if (!player.IsSelectionValid && Judge != player && !player.IsAfk)
					{
						yield return player;
						activeRound = true;
					}
				}

				// If everyone's away, everyone is pending.
				if (!activeRound)
				{
					foreach (var player in _players.ToArray())
					{
						if (!player.IsSelectionValid && Judge != player)
						{
							yield return player;
						}
					}
				}
			}
		}

		/// <summary>
		/// Enumerates played cards for current round.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<(LahPlayer, WhiteCard[])> GetRoundPlays()
		{
			lock (_allPlayersSync)
			{
				foreach (var play in _roundPlays)
				{
					yield return play;
				}
			}
		}

		public IEnumerable<WhiteCard> DrawWhiteCards(int count)
		{
			for (int i = 0; i < count; i++)
			{
				// TODO: Warn admin if not enough cards for players
				if (_whiteDrawPile.Count == 0)
				{
					_whiteDrawPile.AddRange(_whiteDiscardPile);
					_whiteDiscardPile.Clear();
					Shuffle(_whiteDrawPile);
					if (_whiteDrawPile.Count == 0) yield break;
				}

				int cardIndex = _whiteDrawPile.Count - 1;
				var card = _whiteDrawPile[cardIndex];
				_whiteDrawPile.RemoveAt(cardIndex);
				yield return card;
			}
		}

		/// <summary>
		/// Ensures that the player has a full hand, plus an optional number of additional cards.
		/// </summary>
		/// <param name="player">The player to deal to.</param>
		/// <param name="extraCards">(Optional) The number of additional cards to deal.</param>
		private void Deal(LahPlayer player, int extraCards = 0)
		{
			int drawNum = Math.Max(0, Settings.HandSize - player.HandSize + extraCards);
			if (drawNum <= 0) return;
			player.AddToHand(DrawWhiteCards(drawNum));
		}

		/// <summary>
		/// Creates and returns a new player ID.
		/// </summary>
		/// <returns></returns>
		private int CreatePlayerId()
		{
			int id = _nextPlayerId;
			_nextPlayerId = (_nextPlayerId + 1) % int.MaxValue;
			return id;
		}

		/// <summary>
		/// (Stage = <see cref="GameStage.RoundInProgress"/>): Checks if all players have played, and transitions to judging phase if so.
		/// </summary>
		private void CheckRoundPlays()
		{
			lock (_allPlayersSync)
			{
				if (Stage != GameStage.RoundInProgress || _players.All(p => p.IsAfk)) return;
				if (_players.All(p => p.IsSelectionValid || Judge == p || p.IsAfk))
				{
					Stage = GameStage.JudgingCards;
				}
			}
		}

		public Card GetCardById(string id)
		{
			if (String.IsNullOrWhiteSpace(id)) return null;
			return _cards.TryGetValue(id, out var card) ? card : null;
		}

		public string CreatePlayerName(string requestedName, LahPlayer player)
		{
			var namePreSan = requestedName?.Trim() ?? DefaultPlayerName;
			bool asshole = false;
			var sanitizedName = StringUtilities.SanitizeClientString(
				namePreSan,
				Settings.MaxPlayerNameLength,
				ref asshole,
				c => !Char.IsControl(c) && (Char.IsLetterOrDigit(c) || PlayerNameCharExceptions.Contains(c)));

			player.IsAsshole |= asshole;

			if (sanitizedName.Length == 0) sanitizedName = DefaultPlayerName;

			var currentName = sanitizedName;
			int iter = 2;
			while (PlayerNameExists(currentName))
			{
				currentName = $"{sanitizedName} {iter}";
				iter++;
			}
			return currentName;
		}

		private bool PlayerNameExists(string name) => _players.Any(p => p.Name == name);

		/// <summary>
		/// Creates a new play and adds them to the game.
		/// </summary>
		/// <param name="name">The requested player name.</param>
		/// <returns></returns>
		public LahPlayer CreatePlayer(string name = null)
		{
			lock (_allPlayersSync)
			{
				var player = new LahPlayer(this, CreatePlayerId());
				player.Name = CreatePlayerName(name, player);

				// Subscribe events
				player.SelectionChanged += OnPlayerSelectionChanged;
				player.NameChanged += OnPlayerNameChanged;
				player.JudgedCards += OnPlayerJudgedCards;
				player.ScoreChanged += OnPlayerScoreChanged;
				player.AfkChanged += OnPlayerAfkChanged;

				// Give them some cards
				Deal(player);
				player.AddBlankCards(Settings.BlankCards);

				// Add them to the player list
				_players.Add(player);
				RaisePlayerJoined(player);
				return player;
			}
		}

		/// <summary>
		/// Removes a player from the game.
		/// </summary>
		/// <param name="player">The player to remove.</param>
		/// <param name="reason">The reason for removal.</param>
		/// <returns></returns>
		public bool RemovePlayer(LahPlayer player, string reason)
		{
			lock (_allPlayersSync)
			{
				bool success = _players.Remove(player);

				// Unsubscribe events
				player.SelectionChanged -= OnPlayerSelectionChanged;
				player.NameChanged -= OnPlayerNameChanged;
				player.JudgedCards -= OnPlayerJudgedCards;
				player.ScoreChanged -= OnPlayerScoreChanged;
				player.AfkChanged -= OnPlayerAfkChanged;

				// Reclaim their cards
				player.DiscardHand();
				player.DiscardSelection();

				RaisePlayerLeft(player, reason);
				return success;
			}
		}

		/// <summary>
		/// Clears the plays for the round and resets the winning play index.
		/// </summary>
		private void ClearRoundPlays()
		{
			_roundPlays.Clear();
			_winningPlayIndex = -1;
		}

		private void PopulateRoundPlays()
		{
			lock (_allPlayersSync)
			{
				_roundPlays.Clear();
				_roundPlays.AddRange(_players.Where(p => p != Judge).Select(p => (p, p.GetSelectedCards().ToArray())));
				Shuffle(_roundPlays); // mitigate favoritism
			}
		}

		private void CheckMinPlayers()
		{
			if (PlayerCount < Settings.MinPlayers)
			{
				NewGame();
			}
		}

		/// <summary>
		/// Enumerates all black cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<BlackCard> GetBlackCards()
		{
			foreach (var card in _blackCards) yield return card;
		}

		/// <summary>
		/// Enumerates all white cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<WhiteCard> GetWhiteCards()
		{
			foreach (var card in _whiteCards) yield return card;
		}

		/// <summary>
		/// Moves all the contents of the provided hashlist of white cards to the disacard pile.
		/// </summary>
		/// <param name="cards">The cards to discard.</param>
		public void MoveToDiscardPile(HashList<WhiteCard> cards)
		{
			cards.MoveTo(_whiteDiscardPile);
		}

		#region Properties

		/// <summary>
		/// The current stage of the game.
		/// </summary>
		public GameStage Stage
		{
			get => _stage;
			set
			{
				lock (_stageChangeLock)
				{
					var old = _stage;
					_stage = value;
					OnStageChanged(old, value);
				}
			}
		}

		public int BlackCardCount => _blackCards.Count;

		public int WhiteCardCount => _whiteCards.Count;

		public int Round => _roundNum;

		public int PlayerCount => _players.Count;

		public int WinningPlayIndex => _winningPlayIndex;

		public LahPlayer RoundWinner => _winningPlayIndex < 0 || _winningPlayIndex >= _roundPlays.Count ? null : _roundPlays[_winningPlayIndex].Item1;

		public BlackCard CurrentBlackCard => _blackCardIndex >= 0 && _blackCardIndex < _blackCards.Count ? _blackCards[_blackCardIndex] : null;

		public LahPlayer Judge => PlayerCount == 0 || _judgeIndex < 0 || _judgeIndex >= _players.Count ? null : _players[_judgeIndex];

		public LahSettings Settings { get; }

		#endregion

		#region Event Handlers

		private void OnPlayerSelectionChanged(LahPlayer player, WhiteCard[] selection)
		{
			lock (_allPlayersSync)
			{
				if (Stage == GameStage.RoundInProgress && player.IsSelectionValid)
				{
					Console.WriteLine($"{player} selected: {selection.Select(c => c.IsCustom ? $"(Custom) {c.GetContent("en-US")}" : c.ToString()).Aggregate((c, n) => $"{c}, {n}")}");
					CheckRoundPlays();
					Deal(player);
					RaiseStateChanged();
				}
			}
		}

		// Called when a player joins or leaves.
		private void OnPlayerCountChanged()
		{
			switch (Stage)
			{
				case GameStage.GameStarting:
				{
					if (PlayerCount >= Settings.MinPlayers)
					{
						NewRound();
					}
					break;
				}
				case GameStage.RoundInProgress:
				case GameStage.JudgingCards:
				case GameStage.RoundEnd:
				case GameStage.GameEnd:
				{
					CheckMinPlayers();
					break;
				}
			}
			RaisePlayersChanged();
			RaiseStateChanged();
		}

		// Called when a player joins, leaves, or is edited.
		private void OnPlayersChanged()
		{
			RaisePlayersChanged();
		}

		/// <summary>
		/// Called whenever the stage changes.
		/// </summary>
		/// <param name="oldStage">The previous stage as of invocation.</param>
		/// <param name="currentStage">The current stage as of invocation.</param>
		private void OnStageChanged(GameStage oldStage, GameStage currentStage)
		{
			switch (currentStage)
			{
				case GameStage.RoundInProgress:
					ClearRoundPlays();
					break;
				case GameStage.JudgingCards:
					PopulateRoundPlays();
					break;
				case GameStage.RoundEnd:
					if (oldStage == GameStage.JudgingCards)
					{
						SaveRoundPlays();
						UpdateScoreForWinningPlay();
						RoundEndTimeoutAsync();
					}
					break;
				case GameStage.GameEnd:
					GameEndTimeoutAsync();
					break;
				case GameStage.GameStarting:
					ClearRoundPlays();
					break;
			}

			RaiseStageChanged(oldStage, currentStage);
			RaiseStateChanged();
		}

		private void SaveRoundPlays()
		{
			lock(_allPlayersSync)
			{
				foreach(var player in _players)
				{
					player.SaveCurrentPlay(RoundWinner == player);
				}
			}
		}

		private void UpdateScoreForWinningPlay()
		{
			var winningPlayer = RoundWinner;
			if (winningPlayer == null) return;
			winningPlayer.AddPoints(1);
		}

		private async void RoundEndTimeoutAsync()
		{
			if (Stage != GameStage.RoundEnd) return;

			int roundNumForTimeout = _roundNum;

			await Task.Delay(Settings.RoundEndTimeout);

			if (Stage == GameStage.RoundEnd && _roundNum == roundNumForTimeout)
			{
				lock (_allPlayersSync)
				{
					// Check if any of the players have reached the winning score
					if (_players.Any(p => p.Score >= Settings.MaxPoints))
					{
						EndGame();
					}
					else
					{
						NewRound();
					}
				}
			}
		}

		private async void GameEndTimeoutAsync()
		{
			if (Stage != GameStage.GameEnd) return;

			await Task.Delay(Settings.GameEndTimeout);

			if (Stage == GameStage.GameEnd)
			{
				NewGame();
			}
		}

		private void OnPlayerJudgedCards(LahPlayer player, int winningPlayIndex)
		{
			if (!player.CanJudgeCards || winningPlayIndex < 0 || winningPlayIndex >= _roundPlays.Count) return;
			_winningPlayIndex = winningPlayIndex;
			Stage = GameStage.RoundEnd;
			RaiseRoundEnded(Round, RoundWinner);
		}

		private void OnPlayerNameChanged(LahPlayer player, string name)
		{
			OnPlayersChanged();
		}

		private void OnPlayerScoreChanged(LahPlayer player, int points)
		{
			OnPlayersChanged();
		}

		private void OnPlayerAfkChanged(LahPlayer player, bool afk)
		{
			if (afk && Judge == player)
			{
				NextJudge();
			}
			OnPlayersChanged();
			RaiseStateChanged();
		}

		#endregion

		#region Event Raisers

		private void RaiseRoundStarted() => RoundStarted?.Invoke();

		private void RaiseStateChanged() => GameStateChanged?.Invoke();

		private void RaisePlayersChanged() => PlayersChanged?.Invoke();

		private void RaisePlayerJoined(LahPlayer p)
		{
			PlayerJoined?.Invoke(p);
			OnPlayerCountChanged();
		}

		private void RaisePlayerLeft(LahPlayer p, string reason)
		{
			PlayerLeft?.Invoke(p, reason);
			OnPlayerCountChanged();
		}

		private void RaiseRoundEnded(int round, LahPlayer winner) => RoundEnded?.Invoke(round, winner);

		private void RaiseStageChanged(in GameStage oldStage, in GameStage currentStage) => StageChanged?.Invoke(oldStage, currentStage);

		#endregion
	}
}
