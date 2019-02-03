(() => {
    class Card {
        constructor(type, id, content, blanks, pack) {
            this._id = id;
            this._content = content;
            this._type = type;
            this._blanks = blanks;
            this._pack = pack;
        }

        get id() {
            return this._id;
        }

        get type() {
            return this._type;
        }

        get blanks() {
            return this._blanks;
        }

        get pack() {
            return this._pack;
        }

        getContent(langCode) {
            return this._content[langCode] || this._content["en-US"] || "???";
        }

        getLocalContent() {
            return this.getContent(navigator.language);
        }

        toString() {
            this._content["en-US"] || "???";
        }

        static fromObject(json) {
            return new Card(
                json["id"].startsWith("b_") ? "black" : "white",
                json["id"],
                json["content"],
                json["blanks"] || 1,
                json["pack"] || ""
            );
        }
    }

    Array.prototype.indexOfWhere = function(predicate) {
        for(let i = 0; i < this.length; i++) {
            if (predicate(this[i])) {
                return i;
            }
        }
        return -1;
    }

    const STAGE_GAME_STARTING = "game_starting";
    const STAGE_PLAYING = "playing";
    const STAGE_JUDGING = "judging";
    const STAGE_ROUND_END = "round_end";
    const STAGE_GAME_END = "game_end";
    const WS_URL = "ws://" + document.domain + ":3000/lah";

    lah = {};
    lah.round = 0;
    lah.whiteCards = {};
    lah.blackCards = {};
    lah.packMetadata = {};
    lah.playerHand = []; // Card[] - Array of current cards in hand
    lah.playerHandSelection = []; // [{blankIndex:number, id:string}] - Array of client's currently selected cards
    lah.clientPlayedCards = []; // string[] - Array of client's currently played card IDs
    lah.currentBlackCard = null; // Card - Current black card on table
    lah.localPlayerName = ""; // Local player name
    lah.localPlayerId = -1; // Local player ID
    lah.currentJudgeId = -1; // Player ID of current judge
    lah.isClientJudge = false; // boolean - Am I the judge right now?
    lah.roundPlays = []; // string[][] - Array of card IDs played by all users
    lah.stage = STAGE_GAME_STARTING; // Current game stage ID
    lah.pendingPlayers = []; // List of player IDs who still need to play cards
    lah.playerList = [];
    lah.winningPlayerId = -1;
    lah.winningPlayIndex = -1;
    lah.selectedPlayIndex = -1;
    lah.isWaitingOnPlayer = false;
    lah.numBlanks = 0;
    lah.blankCards = []; // Array of strings containing blank card contents

    let gameArea = null;
    let handArea = null;
    let handCardsContainer = null;
    let handCardsScrollArea = null;
    let playArea = null;
    let playCardsArea = null;
    let playCardsScrollArea = null;
    let blackCardArea = null;
    let judgeStatusCardText = null;
    let judgeMessageBody = null;
    let btnPlay = null;
    let btnPick = null;

    let ws = new LahClient(WS_URL, 10000);

    // Sanitizes HTML content for card text
    function createContentHtml(str) {
        return str
            .replace(" & ", " &amp; ")
            .replace("\'", "&apos;")
            .replace("\"", "&quot;")
            .replace("(", "&lpar;")
            .replace(")", "&rpar;")
            .replace(/[\n]/, "<br/>");
    }

    // Make an HTMLElement from the specified Card object
    function makeCardElement(card) {
        let el = document.createElement("card");
        let packInfo = lah.packMetadata[card.pack];
        el.setAttribute("data-card", card.id);
        el.setAttribute(card.type, "");
        
        // Pack info ribbon
        let ribbon = document.createElement("div");
        ribbon.classList.add("ribbon");
        ribbon.setAttribute("data-packname", (packInfo && packInfo.name) || "");
        ribbon.setAttribute("data-packaccent", (packInfo && packInfo.accent) || "black");

        // Card text
        el.innerHTML = createContentHtml(card.getLocalContent());

        el.appendChild(ribbon);

        // Add pick # if applicable
        if (card.blanks > 1) {
            let divPick = document.createElement("div");
            divPick.classList.add("pick");
            let spanPickNum = document.createElement("span");
            spanPickNum.classList.add("num");
            spanPickNum.innerText = card.blanks.toString();
            divPick.appendChild(spanPickNum);
            el.appendChild(divPick);
        }
        
        return el;
    }

    // Make an HTMLElement representing a blank card
    function makeBlankCardElement(index) {
        let el = document.createElement("card");
        el.setAttribute("data-card", "blank");
        el.setAttribute("data-blank-index", index !== undefined ? index : -1);
        el.setAttribute("white", "");

        // Create textarea container
        let txtAreaDiv = document.createElement("div");
        txtAreaDiv.classList.add("blank-card-text-container");
        // Create textarea
        let txtArea = document.createElement("textarea");
        txtArea.setAttribute("aria-label", "Custom card text");
        txtArea.setAttribute("wrap", "hard");
        txtArea.setAttribute("placeholder", "Write your card text here.");
        txtArea.setAttribute("spellcheck", "false");
        txtArea.onclick = e => e.stopPropagation();

        // Add any existing blank card text
        if (index !== undefined && index >= 0 && index < lah.numBlanks) {
            txtArea.value = lah.blankCards[index];
        }

        // Update stored copy of blank card text when textarea is changed
        txtArea.addEventListener("input", () => {
            if (index >= 0 && index < lah.numBlanks) {
                lah.blankCards[index] = txtArea.value.trim();
            }
        });

        txtArea.classList.add("blank-card-text");
        // Add the textarea to its container
        txtAreaDiv.appendChild(txtArea);
        // Add the container to the card
        el.appendChild(txtAreaDiv);

        let logo = document.createElement("div");
        logo.classList.add("logo");
        logo.setAttribute("aria-hidden", "true");
        el.appendChild(logo);
        return el;
    }

    function clearObject(o) {
        Object.keys(o).forEach(k => delete o[k]);
    }

    // Handlers for server message types
    let responseHandlers = {
        "s_allcards": msg => {
            clearObject(lah.whiteCards);
            clearObject(lah.blackCards);
            clearObject(lah.packMetadata);
            msg.packs.forEach(packData => {
                lah.packMetadata[packData.id] = {
                    id: packData.id,
                    name: packData.name,
                    accent: packData.accent || "black"
                };
                packData.cards.forEach(cardData => {
                    cardData.pack = packData.id;
                    let card = Card.fromObject(cardData);
                    if (cardData.id.startsWith("b_")) {
                        lah.blackCards[cardData.id] = card;
                    } else {
                        lah.whiteCards[cardData.id] = card;
                    }
                });
            });
        },
        "s_gamestate": msg => {
            let roundChanged = lah.round !== msg.round;
            let stageChanged = lah.stage != msg.stage;
            lah.round = msg.round;
            lah.stage = msg.stage;
            lah.pendingPlayers = msg.pending_players;
            lah.currentJudgeId = msg.judge;
            lah.roundPlays = msg.plays;
            lah.winningPlayerId = msg.winning_player;
            lah.winningPlayIndex = msg.winning_play;
            lah.isWaitingOnPlayer = msg.pending_players.includes(lah.localPlayerId);
            // Update black card if necessary
            updateBlackCard(msg.black_card);
            // Update stage-related stuff
            onStateChanged();
            // Update play area
            updatePlayedCardsArea();

            if (roundChanged) {
                onRoundChanged();
            }

            if (stageChanged) {
                onStageChanged(msg.stage);
            }
        },
        "s_players": msg => {
            lah.playerList = msg.players;
            onPlayerListChanged();
        },
        "s_clientinfo": msg => {
            lah.localPlayerId = msg.player_id;
            setPlayerName(msg.player_name, true);
        },
        "s_hand": msg => {
            let prevNumBlanks = lah.numBlanks;
            lah.playerHand = msg.hand;
            lah.numBlanks = msg.blanks;    
            if (lah.blankCards.length > msg.blanks) {
                lah.blankCards.length = msg.blanks;
            } else if (lah.blankCards.length < msg.blanks) {
                while (lah.blankCards.length < msg.blanks) {
                    lah.blankCards.push("");
                }
            }
            updateHandCardsArea();
        },
        "s_cardsplayed": msg => {
            lah.clientPlayedCards = msg.selection;
            onPlayedCardsChanged();
            onStateChanged();
        }
    };

    function getRoundCardFromId(cardId) {
        let customMatch = cardId.match(/^\s*custom:\s*(.*)\s*$/m);
        let card = null;
        if (customMatch) {
            card = new Card("white", cardId, {"en-US": customMatch[1] || "???"});
        } else {
            card = lah.whiteCards[cardId];
        }
        return card;
    }

    // Update the play area to contain the right cards according to the game stage
    function updatePlayedCardsArea() {
        playCardsArea.killChildren();
        switch (lah.stage) {
            case STAGE_PLAYING:
                if (!lah.isClientJudge) {
                    for (let cardId of lah.clientPlayedCards) {
                        let card = getRoundCardFromId(cardId);
                        let e = makeCardElement(card);
                        playCardsArea.appendChild(e);
                    }
                }
                break;
            case STAGE_JUDGING:
                {
                    let i = 0;
                    for (let play of lah.roundPlays) {
                        let groupElement = document.createElement("div");
                        groupElement.classList.add("card-group");
                        groupElement.setAttribute("data-play-index", i);
                        let playIndex = i;
                        groupElement.onclick = () => onPlayGroupClicked(playIndex, groupElement);
                        for (let cardId of play) {
                            let cardElement = makeCardElement(getRoundCardFromId(cardId));
                            groupElement.appendChild(cardElement);
                        }
                        playCardsArea.appendChild(groupElement);
                        i++;
                    }
                    break;
                }
            case STAGE_ROUND_END:
                {
                    let i = 0;
                    for (let play of lah.roundPlays) {
                        if (i == lah.winningPlayIndex) {
                            let groupElement = document.createElement("div");
                            groupElement.classList.add("card-group");
                            groupElement.setAttribute("data-play-index", i);
                            groupElement.classList.add("winner");

                            for (let cardId of play) {
                                let cardElement = makeCardElement(getRoundCardFromId(cardId));
                                groupElement.appendChild(cardElement);
                            }
                            playCardsArea.appendChild(groupElement);
                            break;
                        }

                        i++;
                    }
                    break;
                }
        }
    }

    // Repopulates hand cards
    function updateHandCardsArea() {
        handCardsContainer.killChildren();

        // Add all white cards in hand
        for (let cardId of lah.playerHand) {
            let card = lah.whiteCards[cardId];
            let id = cardId;
            if (card) {
                let e = makeCardElement(card);
                e.onclick = () => onHandCardClicked(id, e);
                handCardsContainer.appendChild(e);
            }
        }

        // Add all blank cards in hand
        for(let i = 0; i < lah.numBlanks; i++) {
            let e = makeBlankCardElement(i);
            let blankIndex = i;
            e.onclick = () => onHandCardClicked(null, e, blankIndex);
            handCardsContainer.appendChild(e);
        }
    }

    // Sets the current black card to the card with the specified ID
    function updateBlackCard(cardId) {
        if (lah.currentBlackCard && lah.currentBlackCard.id == cardId) return;
        blackCardArea.killChildren();
        lah.currentBlackCard = lah.blackCards[cardId] || null;
        if (lah.currentBlackCard) {
            let e = makeCardElement(lah.currentBlackCard);
            blackCardArea.appendChild(e);
        }
    }

    // Raised when a card in the play area is clicked by a judge
    function onPlayGroupClicked(playIndex, groupElement) {
        let canJudge = lah.isClientJudge && lah.stage == STAGE_JUDGING;
        if (!canJudge) return;
        lah.selectedPlayIndex = playIndex;
        onJudgeSelectionChanged();
        updateJudgeCardSelection();
        updateUiState();
    }

    function selectionContainsCard(cardId) {
        if (cardId === undefined || cardId === null) return false;
        return lah.playerHandSelection.find(s => cardId && s.id == cardId) !== undefined;
    }

    function selectionContainsBlank(blankIndex) {
        if (blankIndex === undefined || blankIndex < 0 || blankIndex >= lah.numBlanks) return false;
        return lah.playerHandSelection.find(s => s.blankIndex === blankIndex) !== undefined;
    }

    // Raised when a card in the player's hand is clicked
    function onHandCardClicked(cardId, cardElement, blankCardIndex) {
        // deselecting
        if (selectionContainsCard(cardId) || selectionContainsBlank(blankCardIndex)) {
            let removeIndex = lah.playerHandSelection.indexOfWhere(s => (cardId && s.id === cardId) || s.blankIndex === blankCardIndex);
            if (removeIndex >= 0) lah.playerHandSelection.splice(removeIndex, 1);
            console.log(blankCardIndex);
        }
        // selecting
        else {
            // Make sure the selection is not too big
            if (lah.playerHandSelection.length >= lah.currentBlackCard.blanks) {
                lah.playerHandSelection.splice(0, lah.playerHandSelection.length - lah.currentBlackCard.blanks + 1);
            }
            lah.playerHandSelection.push({id: cardId, blankIndex: blankCardIndex});
        }
        onSelectionChanged();
        updateHandCardSelection();
    }

    // Sets the status bar text to a specific string
    function setStatusText(statusText) {
        document.getElementById("status").innerHTML = statusText;
    }

    // Ensures that the selection numbers on the hand card elements are accurate
    function updateHandCardSelection() {
        var cardElements = Array.from(handCardsContainer.children).filter(c => c.tagName.toLowerCase() == "card");
        if (cardElements.length == 0) return;
        for (let el of cardElements) {
            el.removeAttribute("data-selection");
        }

        for (let i = 0; i < lah.playerHandSelection.length; i++) {
            let selection = lah.playerHandSelection[i];
            let el = cardElements.find(e => { 
                let attrBlank = e.getAttribute("data-blank-index");
                let attrCardId = e.getAttribute("data-card");
                return (attrCardId === selection.id) || (attrCardId === "blank" && attrBlank == selection.blankIndex);
            });
            if (el) {
                el.setAttribute("data-selection", i + 1);
            }
        }
    }

    function updateJudgeCardSelection() {
        var groupElements = Array.from(playCardsArea.children).filter(c => c.classList.contains("card-group"));
        if (groupElements.length == 0) return;

        for (let el of groupElements) {
            let attrIndex = el.getAttribute("data-play-index");
            el.setClass("judge-selected", attrIndex == lah.selectedPlayIndex);
        }
    }

    function setPlayerName(name, noSave) {
        if (name == lah.localPlayerName) return;
        lah.localPlayerName = name;
        if (!noSave) Cookies.set("name", name, { expires: 365 });
        onPlayerNameChanged();
    }

    function loadOptions(name) {
        lah.localPlayerName = Cookies.get("name") || "Player";
        document.getElementById("txt-username").value = lah.localPlayerName;
    }

    // Sends c_clientinfo message to the server
    function sendClientInfo() {
        const requestedName = document.getElementById("txt-username").value;
        sendMessage({
            "msg": "c_updateinfo",
            "userinfo": {
                "name": requestedName
            }
        });
    }

    lah.judgeCards = function (playIndex) {
        sendMessage({
            "msg": "c_judgecards",
            "play_index": playIndex
        });
    }

    applyOptions = function () {
        hideModal("modal-options");
        sendClientInfo();
    }

    // Sends JSON message to server
    function sendMessage(msg) {
        console.log("Sending " + msg.msg);
        ws.send(msg);
    }

    // Raised when connection closes
    function onConnectionClosed() {
        console.log("disconnected");
        setStatusText("Not connected");
    };

    // Raised when connection opens
    function onConnectionOpened() {
        console.log("connected");
    }

    // Raised when the websocket receives a message
    function onDataReceived(data) {
        let json = JSON.parse(data);
        let type = json["msg"];
        // console.log("Received " + type);
        let handler = responseHandlers[type];
        if (handler) {
            handler(json);
        }
    };

    function onPlayerListChanged() {

    }

    // Make sure the correct elements are visible/enabled
    function updateUiState() {
        let handEnabled = lah.isWaitingOnPlayer && !lah.isClientJudge;
        let pendingPlayerCount = lah.pendingPlayers.length;

        gameArea.setClass("lah-stage-game-starting", lah.stage == STAGE_GAME_STARTING);
        gameArea.setClass("lah-stage-playing", lah.stage == STAGE_PLAYING);
        gameArea.setClass("lah-stage-judging", lah.stage == STAGE_JUDGING);
        gameArea.setClass("lah-stage-round-end", lah.stage == STAGE_ROUND_END);
        gameArea.setClass("lah-stage-gane-end", lah.stage == STAGE_GAME_END);
        gameArea.setClass("lah-judge", lah.isClientJudge);

        switch (lah.stage) {
            case STAGE_GAME_STARTING:
                disable("hand-cards-area");
                disable("btn-play");
                btnPlay.setVisible(false);
                playArea.setVisible(false);
                handArea.setVisible(false);
                break;
            case STAGE_PLAYING:
                setEnabled("hand-cards-area", handEnabled);
                setEnabled("btn-play", handEnabled);
                btnPlay.setVisible(lah.isWaitingOnPlayer);
                handArea.setVisible(lah.isWaitingOnPlayer && !lah.isClientJudge);
                playArea.setVisible(!lah.isWaitingOnPlayer && !lah.isClientJudge);

                if (lah.isClientJudge) {
                    judgeStatusCardText.innerHTML = pendingPlayerCount.toString();
                    if (pendingPlayerCount > 3) {
                        judgeMessageBody.innerHTML = "<span class='highlight'>You're the Card Czar.</span><br/><smaller>Waiting for other players...</smaller>";
                    } else {
                        let strRemainingPlayers = "";
                        for (let i = 0; i < pendingPlayerCount; i++) {
                            if (pendingPlayerCount > 1) {
                                if (i > 0) strRemainingPlayers += pendingPlayerCount > 2 ? ", " : " ";
                                if (i == pendingPlayerCount - 1) strRemainingPlayers += "and ";
                            }
                            let p = lah.playerList.find(p => p.id == lah.pendingPlayers[i]);
                            strRemainingPlayers += (p && p.name) || "???";
                        }
                        judgeMessageBody.innerHTML = "<span class='highlight'>You're the Card Czar.</span><br/><smaller>Waiting for " + strRemainingPlayers + "...</smaller>";
                    }
                }
                break;
            case STAGE_JUDGING:
                disable("hand-cards-area");
                disable("btn-play");
                btnPlay.setVisible(false);
                playArea.setVisible(true);
                handArea.setVisible(false);
                setEnabled("btn-judge-pick", lah.stage == STAGE_JUDGING && lah.isClientJudge && lah.selectedPlayIndex > -1);
                break;
            case STAGE_ROUND_END:
                disable("hand-cards-area");
                disable("btn-play");
                btnPlay.setVisible(false);
                playArea.setVisible(true);
                handArea.setVisible(false);
                break;
        }

        handCardsScrollArea.updateScrollTracking();
        playCardsScrollArea.updateScrollTracking();
    }

    // Called when game state is updated via s_gamestate
    function onStateChanged() {
        updateJudgeInfo();
        updateUiState();
        updateStatus();
        onSelectionChanged();
    }

    // Called when s_cardsplayed received
    function onPlayedCardsChanged() {
        updatePlayedCardsArea();
    }

    // Updates the lah.isClientJudge flag
    function updateJudgeInfo() {
        if (!lah.isClientJudge) {
            if (lah.currentJudgeId == lah.localPlayerId) {
                lah.isClientJudge = true;
                lah.selectedPlayIndex = -1;
            }
        } else {
            if (lah.currentJudgeId != lah.localPlayerId) {
                lah.isClientJudge = false;
            }
        }
    }

    // Sets the status bar text to a string determined by the local game state
    function updateStatus() {
        if (!ws.isOpen) {
            let ckName = Cookies.get("name");
            setStatusText(ckName ? "Hello, " + ckName : "Hello");
            return;
        }

        switch (lah.stage) {
            case STAGE_GAME_STARTING:
                setStatusText("Waiting for players to join");
                break;
            case STAGE_PLAYING:
                setStatusText("Round " + lah.round);
                break;
            case STAGE_JUDGING:
                if (lah.isClientJudge) {
                    setStatusText("Choose the best play.");
                } else {
                    setStatusText("Card Czar is deciding...");
                }
                break;
            case STAGE_ROUND_END:
                if (lah.winningPlayerId == lah.localPlayerId) {
                    setStatusText("<span class='highlight'>You won the round!</span>");
                } else {
                    let winningPlayer = lah.playerList.find(p => p.id == lah.winningPlayerId);
                    if (winningPlayer) {
                        setStatusText(winningPlayer.name + " won the round!");
                    } else {
                        setStatusText("Nobody won the round");
                    }
                }
                break;
        }
    }

    function onStageChanged(stage) {
        switch (stage) {
            case STAGE_PLAYING:
                {
                    if (lah.isClientJudge) {
                        showBannerMessage("Round " + lah.round + "<br/><small>You're the Card Czar.</small>");
                    } else {
                        let judge = lah.playerList.find(p => p.id == lah.currentJudgeId);
                        let judgeName = (judge && judge.name) || "Nobody";
                        showBannerMessage("Round " + lah.round + "<br><small>" + judgeName + " is the Card Czar.</small>");
                    }
                    break;
                }
            case STAGE_ROUND_END:
                {
                    if (lah.winningPlayerId == lah.localPlayerId) {
                        showBannerMessage("You won the round!", 3);
                    } else {
                        let winningPlayer = lah.playerList.find(p => p.id == lah.winningPlayerId);
                        if (winningPlayer) {
                            showBannerMessage(winningPlayer.name + " wins the round!", 3);
                        } else {
                            showBannerMessage("Round winner left, so nobody scores!", 3);
                        }
                    }
                }
        }
    }

    // Raised when the PLAY button is clicked
    function onPlayClicked() {        
        sendMessage({ 
            msg: "c_playcards", 
            cards: lah.playerHandSelection.map(s => {
                if (s.id) {
                    return s.id;
                } else if (s.blankIndex !== undefined && s.blankIndex >= 0 && s.blankIndex < lah.numBlanks) {
                    return "custom: " + lah.blankCards[s.blankIndex];
                } else {
                    return null;
                }
            }) 
        });

        lah.playerHandSelection.length = 0;
        updateHandCardSelection();
    };

    // Raised when the PICK button is clicked
    function onJudgePickClicked() {
        lah.judgeCards(lah.selectedPlayIndex);
    }

    function onPlayerNameChanged() {
        document.getElementById("txt-username").value = lah.localPlayerName;
    }

    function onSelectionChanged() {
        setEnabled("btn-play", (lah.currentBlackCard && lah.playerHandSelection.length == lah.currentBlackCard.blanks));
    }

    function onRoundChanged() {

    }

    function onJudgeSelectionChanged() {

    }

    window.onbeforeunload = function (e) {
        ws.close(1000, "Exiting game");
        return null;
    }

    lah.start = function () {
        console.log("client started");

        // Populate saved options
        loadOptions();

        // Get game elements
        gameArea = document.getElementById("game");
        handCardsContainer = document.getElementById("hand-cards-container");
        handCardsScrollArea = document.getElementById("hand-cards-scroll-area");
        handArea = document.getElementById("hand-area");
        playCardsArea = document.getElementById("play-cards-area");
        playCardsScrollArea = document.getElementById("play-cards-scroll-area");
        playArea = document.getElementById("play-area");
        btnPlay = document.getElementById("btn-play");
        btnPick = document.getElementById("btn-judge-pick");
        blackCardArea = document.getElementById("black-card-area");
        judgeStatusCardText = document.getElementById("judge-status-card-text");
        judgeMessageBody = document.getElementById("judge-message-body");

        updateStatus();
        updateUiState();

        // Set events
        btnPlay.onclick = onPlayClicked;
        btnPick.onclick = onJudgePickClicked;

        if ("WebSocket" in window) {
            ws.onclose = onConnectionClosed;
            ws.onopen = onConnectionOpened;
            ws.onmessage = onDataReceived;
            ws.connect();
        } else {
            showModal("modal-no-ws");
        }
    }
})();                                                                                                                                                                                                                                                                    