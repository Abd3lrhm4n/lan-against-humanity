# LAN Against Humanity

A Cards Against Humanity clone that you can host on your home network.
It also works on your mobile phone!

**This software is under active development and is not intended for use as of now. Please don't expect a fully-featured product at this point.**


## Features

* **Completely Local** - Host on any old LAN. No Internet connection necessary.
* **Custom Decks** - Write your own decks using a simple JSON format. Mix and match cards by adding multiple decks to your server.
* **Localizable Cards** - Cards can be written in multiple languages and your device will display them in your set browser language. You can even have several people in a game using the same cards in different languages!
* **Trophies** - At the end of each game, see what kind of awful each of your friends is.


## How it works

The game server consists of a NancyFx web server and WebSocket server. The web server dishes out the webapp to anyone accessing the game in a browser. The webapp connects to the WebSocket server, which connects players to the actual game.


## FAQ

### Why?

I was bored and wanted a fun project to work on over winter break.

### But we already have things like PYX, Azala, etc.  Why another one?

And both of those are great! There's nothing wrong with them. As mentioned above, this is just a project I did for fun and decided was worth sharing.

### Can I add my own cards?

Yes, decks are written using a simple JSON format. Add them to the `decks` folder before starting up the server.

### Can I host this on a public webserver?

I really don't recommend it. It's only designed to host one game at a time. It also doesn't support HTTPS at the moment, so frankly that would be a pretty unwise thing to do.

### Your webdev skills suck! I could do so much better!

Isn't that the great thing about open source?

### You should be using React/Angular/Bootstrap/etc.

No.

### There's a feature I want you to add.

Submit an issue and we'll see what can be done.

### Why don't you include the CAH cards?

CAH is licensed under a CC BY-NC-SA 2.0 license.
If I distributed my software with their IP included, I would have to place all of my code under that same license. Since Creative Commons is not designed for software,
it's much easier for everyone if I decouple the CAH content from the game.

## Legal

LAN Against Humanity is a clone of Cards Against Humanity. The original game, available at [cardsagainsthumanity.com](https://cardsagainsthumanity.com), is available under a [CC BY-NC-SA license](https://creativecommons.org/licenses/by-nc-sa/2.0/). This project is in no way endorsed or sponsored by Cards Against Humanity. For full license information, including third-party software licenses, see [LICENSE](LICENSE).