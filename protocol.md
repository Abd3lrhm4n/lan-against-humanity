
# LAN Against Humanity Protocol

## Overview

LAN Against Humanity clients use the WebSocket protocol to communicate with the server.

The server listens for client connections on `./lah:3000` by default.

Clients are provided certain query string options:

* `name`: Sets your name to the specified string upon successful connection.

There is no explicit concept of 'responses' in LAH's protocol. While the server may send a message in response to a particular client message, it is not required to explicitly state that it is a response.

Messages are encoded as UTF-8 JSON objects. Each object must contain at least a `msg` property, which is a string describing what kind of message is being sent.

Client message names are prefixed with `c_` and server messages with `s_` for clarity when discussing message types.

## Cards

In order to keep messages as small as possible, cards are only referenced by their ID strings during play.
ID strings must follow the following conventions:

* Non-custom IDs may only contain alphanumeric characters and underscores.
* White card IDs start with `w_`.
* Black card IDs start with `b_`.
* Custom card IDs start with `custom:` followed by the card text.

## Client messages

### c_playcards

Sent by the client during an active game when the player has selected their white card(s) for the current round. The cards are represented using their card IDs, and are in the order of the blanks they are to appear in.

```json
{
    "msg": "c_playcards",
    "cards": ["w_example1", "w_example2"]
}
```

### c_judgecards

Sent by the client when the client's player is judging the current round and they have selected the winning play. 
The round winner is identified by the index of the play from the play list provided by the server.
The client does not know whose play it is until the round ends.

```json
{
    "msg": "c_judgecards",
    "play_index": 0
}
```

### c_updateinfo

Send by the user to update client-specific information, such as username.

List of possible user info keys:
|Key|Description|
|`name`|Name of player.|

```json
{
    "msg": "c_updateinfo",
    "userinfo": {
        "name": "Berkin"
    }
}
```

## Server messages

### s_allcards

The server sends this to a client in response to a `c_getallcards` message. It contains every card in use by the current game. Black and white cards are listed in separate arrays for convenience.

```json
{
    "msg": "s_allcards",
    "white": [
        {
            "id": "w_example",
            "content": {
                "en-US": "An example card.",
                "de-DE": "Eine Beispielkarte."
            }
        }
    ],
    "black": [
        {
            "id": "b_example",
            "content": {
                "en-US": "Nothing is worse than finding ______ in your mailbox.",
                "de-DE": "Nichts ist schlimmer, als ______ im Briefkasten zu entdecken."
            },
            "blanks": 1
        }
    ]
}
```

### s_gamestate

Sent by the server to inform the client which stage the game is currently in.

The possible strings for the `stage` property are listed below:

|Stage ID|Description|
|--------|-----------|
|`game_starting`|The game is starting and waiting for players.|
|`playing`|A round has started and players are choosing cards.|
|`judging`|Judging of the round has begun and the judge will be prompted.|
|`round_end`|The round has concluded and the winning play is displayed.|
|`game_end`|The game has concluded and the winning player is displayed.|

```json
{
    "msg": "s_gamestate",
    "stage": "game_starting",

    "round": 1, // Round number. (all stages)

    // Black card for current round. (playing, judging, round_end, game_end)
    "black_card": "b_example",

    // Player ID of round judge. (playing, judging, round_end, game_end)
    "judge": 123,

    // White cards played. (judging, round_end)
    "plays": [["w_example1"], ["w_example2"]],

    // Players that haven't played their cards yet. (playing)
    "pending_players": [123, 456],

    // Index of winning play. (round_end, game_end)
    "winning_play": 0,

    // ID of round winner. (round_end)
    "winning_player": 123,

    // Final results of game. Null if stage is not game_end.
    "game_results": {
        "winners": [123], // multiple if tie
        "trophies": [
            {
                "player_id": 123,
                "trophy_id": "trophy_racist"
            }
        ]
    }
}
```

### s_players

Sent by the server to provide clients with the current list of players and their information.

```json
{
    "msg": "s_players",
    "players": [
        {
            "name": "Berkin",
            "player_id": 123,
            "score": 0
        }
    ]
}
```

### s_hand

Sent to a client to inform them of the current contents of their hand.

```json
{
    "msg": "s_hand",
    "blanks": 2, // Number of available blank cards
    "hand": ["w_example1", "w_example2"]
}
```

### s_cardsplayed

Sent to a client to inform them of the cards they have played for the current round.

```json
{
    "msg": "s_cardsplayed",
    "selection": ["w_example1", "w_example2"]
}
```

### s_clientinfo

Contains player information that identifies a client. Sent by server when the player changes their name.

```json
{
    "msg": "s_clientinfo",
    "player_id": 123,
    "player_name": "Berkin"
}
```