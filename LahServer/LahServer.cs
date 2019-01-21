using LahServer.Game;
using System;
using WebSocketSharp.Server;

namespace LahServer
{
    public sealed class LahServer : IDisposable
    {
        private readonly WebSocketServer _ws;
        private readonly LahGame _game;
		private bool _disposed = false;

        public LahServer(LahGame game)
        {
            _game = game;
            _ws = new WebSocketServer("ws://0.0.0.0:3000");
        }

        public void Start()
        {
            _ws.AddWebSocketService("/lah", () => new LahClientConnection(_game));
            _ws.Start();
			Console.WriteLine("WebSocket server active");
        }

        public void Stop()
        {
            _ws.Stop();
        }

        public void Dispose()
        {
			if (_disposed) return;

			_disposed = true;
        }
    }
}
