using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game
{
	public delegate void PlayerCardsChangedEventDelegate(LahPlayer player, WhiteCard[] cards);
	public delegate void PlayerSelectionChangedEventDelegate(LahPlayer player, WhiteCard[] selection);
	public delegate void PlayerScoreChangedEventDelegate(LahPlayer player, int points);
	public delegate void PlayerNameChangedEventDelegate(LahPlayer player, string name);
	public delegate void PlayerJudgedCardsEventDelegate(LahPlayer player, int winningPlayIndex);
	public delegate void PlayerAfkChangedEventDelegate(LahPlayer player, bool afk);

	// TODO: Develop pattern for combining *Changed events to reduce unnecessary client updates
	public sealed class LahPlayer
	{
		private readonly HashList<WhiteCard> _hand;
		private readonly HashList<WhiteCard> _selectedCards;
		private int _score;
		private string _name = "Player";
		private bool _afk;
		private int _blankCardsRemaining;
		private readonly object _blankCardLock = new object();

		public event PlayerCardsChangedEventDelegate CardsChanged;
		public event PlayerSelectionChangedEventDelegate SelectionChanged;
		public event PlayerScoreChangedEventDelegate ScoreChanged;
		public event PlayerNameChangedEventDelegate NameChanged;
		public event PlayerJudgedCardsEventDelegate JudgedCards;
		public event PlayerAfkChangedEventDelegate AfkChanged;

		internal LahPlayer(LahGame game, int id)
		{
			Game = game;
			Id = id;
			_hand = new HashList<WhiteCard>();
			_selectedCards = new HashList<WhiteCard>();
		}

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				RaiseNameChanged(value);
			}
		}

		public LahGame Game { get; }

		public int Id { get; }

		public int Score => _score;

		public bool IsAfk
		{
			get => _afk;
			set
			{
				if (_afk != value)
				{
					_afk = value;
					RaiseAfkChanged(value);
				}
			}
		}

		public bool IsAsshole { get; set; }

		public int RemainingBlankCards => _blankCardsRemaining;

		/// <summary>
		/// Adds cards to the player's hand. This does not remove cards from the game's draw pile.
		/// </summary>
		/// <param name="cards">The cards to add.</param>
		public void AddToHand(IEnumerable<WhiteCard> cards)
		{
			_hand.AddRange(cards);
			RaiseCardsChanged();
		}

		/// <summary>
		/// Removes cards from the player's hand. This does not add cards to the game's discard pile.
		/// </summary>
		/// <param name="cards">The cards to remove.</param>
		public void RemoveFromHand(IEnumerable<WhiteCard> cards)
		{
			_hand.RemoveRange(cards);
			RaiseCardsChanged();
		}

		/// <summary>
		/// Enumerates the player's current hand.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<WhiteCard> GetCurrentHand()
		{
			foreach (var card in _hand.ToArray())
			{
				yield return card;
			}
		}

		/// <summary>
		/// Updates the player's selected cards to the specified cards and raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="cards">The cards to select. They must be present in the hand at the time of calling.</param>
		/// <returns></returns>
		public bool PlayCards(IEnumerable<WhiteCard> cards)
		{
			if (!CanPlayCards) return false;
			var cardArray = cards.ToArray();

			// Count how many custom cards they are playing
			int numCustomCards = cards.Count(c => c.IsCustom);

			// Make sure they have enough custom cards for what they're requesting
			if (RemainingBlankCards < numCustomCards) return false;

			// Make sure they own all the cards they want to play, and that they are playing the correct number of cards
			if (cardArray.Length != Game.CurrentBlackCard.BlankCount || cardArray.Any(c => !c.IsCustom && !HasWhiteCard(c))) return false;

			RemoveBlankCards(numCustomCards);
			_hand.RemoveRange(cards);
			_selectedCards.Clear();
			_selectedCards.AddRange(cardArray);
			RaiseCardsChanged();
			RaiseSelectionChanged();
			return true;
		}

		public bool JudgeCards(int winningPlayIndex)
		{
			if (!CanJudgeCards) return false;
			RaiseJudgedCards(winningPlayIndex);
			return true;
		}

		public void AddBlankCards(int numBlankCards)
		{
			lock (_blankCardLock)
			{
				if (numBlankCards <= 0) return;
				_blankCardsRemaining += numBlankCards;
				RaiseCardsChanged();
			}
		}

		public void RemoveBlankCards(int numBlankCards)
		{
			lock(_blankCardLock)
			{
				if (numBlankCards == 0 || _blankCardsRemaining < numBlankCards) return;
				_blankCardsRemaining -= numBlankCards;
				RaiseCardsChanged();
			}
		}

		/// <summary>
		/// Enumerates the player's currently selected cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<WhiteCard> GetSelectedCards() => _selectedCards.ToArray();

		/// <summary>
		/// Indicates whether the player's current selection is valid for the game's current black card.
		/// </summary>
		public bool IsSelectionValid => Game.CurrentBlackCard.BlankCount > 0 && _selectedCards.Count > 0 && _selectedCards.Count == Game.CurrentBlackCard.BlankCount;

		public bool HasWhiteCard(WhiteCard card) => _hand.Contains(card);

		/// <summary>
		/// Indicates whether the player is currently allowed to play cards.
		/// </summary>
		public bool CanPlayCards => Game.Stage == GameStage.RoundInProgress && _selectedCards.Count == 0 && Game.Judge != this;

		public bool CanJudgeCards => Game.Stage == GameStage.JudgingCards && Game.Judge == this;

		/// <summary>
		/// Adds Awesome Points to the player's score.
		/// </summary>
		/// <param name="points">The number of Awesome Points to add. Use a negative number to remove points.</param>
		public void AddPoints(int points)
		{
			_score += points;
			if (points != 0) RaiseScoreChanged();
		}

		/// <summary>
		/// Dumps the player's cards to the game discard pile.
		/// </summary>
		public void DiscardHand()
		{
			Game.MoveToDiscardPile(_hand);
			RaiseCardsChanged();
		}

		/// <summary>
		/// Moves the player's selected cards to the discard pile.
		/// </summary>
		public void DiscardSelection()
		{
			Game.MoveToDiscardPile(_selectedCards);
			RaiseSelectionChanged();
		}

		private void RaiseCardsChanged()
		{
			CardsChanged?.Invoke(this, _hand.ToArray());
		}

		private void RaiseScoreChanged()
		{
			ScoreChanged?.Invoke(this, _score);
		}

		private void RaiseSelectionChanged()
		{
			SelectionChanged?.Invoke(this, _selectedCards.ToArray());
		}

		private void RaiseJudgedCards(int winIndex)
		{
			JudgedCards?.Invoke(this, winIndex);
		}

		private void RaiseNameChanged(string name)
		{
			NameChanged?.Invoke(this, name);
		}

		private void RaiseAfkChanged(bool afk)
		{
			AfkChanged?.Invoke(this, afk);
		}

		public int HandSize => _hand.Count;

		public override string ToString() => $"{Name} (#{Id})";
	}
}
