using LahServer.Game;
using LahServer.Game.Converters;
using LahServer.Web;
using Nancy.Hosting.Self;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LahServer
{
    class Program
    {
		private static LahGame _game;

        static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>() { new CardConverter() }
            };

            var decks = new List<Pack>();

			// Load the settings
			var settings = LahSettings.FromFile("settings.json");

            // Load all the decks
            foreach(var deckPath in Directory.EnumerateFiles("decks", "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var deck = JsonConvert.DeserializeObject<Pack>(File.ReadAllText(deckPath));
                    decks.Add(deck);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load deck '{deckPath}': {ex.Message}");
                }
            }

            _game = new LahGame(decks, settings);

			Console.WriteLine("\n========= GAME STARTING =========\n");
			Console.WriteLine($"Player limit: [{settings.MinPlayers}, {settings.MaxPlayers}]");
			Console.WriteLine($"Hand size: {settings.HandSize}");
			Console.WriteLine($"Perma-Czar: {settings.PermanentCzar}");
			Console.WriteLine($"Cards: {_game.BlackCardCount + _game.WhiteCardCount} ({_game.WhiteCardCount}x white, {_game.BlackCardCount}x black)");
			Console.WriteLine();
			Console.WriteLine($"Packs:\n{decks.Select(d => $"        [{d}]").Aggregate((c, n) => $"{c}\n{n}")}");
			Console.WriteLine("\n=================================\n");

			_game.GameStateChanged += OnGameStateChanged;
			_game.RoundStarted += OnGameRoundStarted;
			_game.StageChanged += OnGameStageChanged;
			_game.RoundEnded += OnGameRoundEnded;

            var hostCfg = new HostConfiguration
            {
                RewriteLocalhost = true,
                UrlReservations = new UrlReservations()
                {
                    CreateAutomatically = true
                }
            };
            
            using (var host = new NancyHost(new Uri(settings.Host), new LahBootstrapper(), hostCfg))
            using (var gameServer = new LahServer(_game))
            {
                gameServer.Start();
                host.Start();
                Console.WriteLine($"Hosting on {settings.Host}");
                Console.ReadLine();
                Console.WriteLine("Stopping...");
                gameServer.Stop();
            }
        }

		private static void OnGameRoundEnded(int round, LahPlayer roundWinner)
		{
			Console.WriteLine($"Round {round} ended: {roundWinner?.ToString() ?? "Nobody"} wins!");
		}

		private static void OnGameStageChanged(in GameStage oldStage, in GameStage currentStage)
		{
			Console.WriteLine($"Stage changed: {oldStage} -> {currentStage}");
		}

		private static void OnGameRoundStarted()
		{
			Console.WriteLine($"ROUND {_game.Round}:");
			Console.WriteLine($"Current black card: {_game.CurrentBlackCard}");
			Console.WriteLine($"Judge is {_game.Judge}");
		}

		private static void OnGameStateChanged()
		{
			UpdateTitle();
		}

		private static void UpdateTitle()
		{
			Console.Title = $"LAN Against Humanity Server ({_game.PlayerCount})";
		}
    }
}
