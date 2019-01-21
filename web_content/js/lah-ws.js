class LahClient {
    constructor(url, retryTime) {
        this._url = url;
        this._retryTime = retryTime || 10000;
        this._userOnOpen = null;
        this._userOnClose = null;
        this._userOnMessage = null;
        this._sendQueue = [];
        this._isOpen = false;
        this._hasError = false;
        this._retryTimer = null;
        this._manualClose = false;
    }

    _onOpen() {
        this._isOpen = true;
        this._hasError = false;
        this._stopRetry();
        if (this._userOnOpen) this._userOnOpen();
    };

    _onClose() {
        this._isOpen = false;
        this._ws = null;
        if (this._userOnClose) this._userOnClose();

        if (!this._manualClose) {
            this._startRetry();
        }
    }

    _onMessage(msg) {
        if (this._userOnMessage) this._userOnMessage(msg.data);
    }

    _onError(error) {
        console.log("WS error #" + error.code);
        this._hasError = true;
    }

    _createWebsocket() {
        let cl = this;
        this._ws = new WebSocket(this._url);
        this._ws.onopen = () => cl._onOpen();
        this._ws.onclose = () => cl._onClose();
        this._ws.onmessage = (msg) => cl._onMessage(msg);
        this._ws.onerror = (error) => cl._onError(error);
    }

    _startRetry() {
        this._stopRetry();
        console.log("retrying connection in " + (this._retryTime / 1000) + "s");
        this._retryTimer = setTimeout(() => this._retry(), this._retryTime);
    }

    _stopRetry() {
        if (!this._retryTimer) return;
        clearTimeout(this._retryToken);
        this._retryTimer = null;
    }

    _retry() {
        console.log("retrying connection...");
        this.connect();
    }

    get isOpen() {
        return this._ws && this._isOpen && this._ws.readyState == WebSocket.OPEN;
    }

    get onopen() {
        return this._userOnOpen;
    }

    set onopen(handler) {
        this._userOnOpen = handler;
    }

    get onclose() {
        return this._userOnClose;
    }

    set onclose(handler) {
        this._userOnClose = handler;        
    }

    get onmessage() {
        return this._userOnMessage;
    }

    set onmessage(handler) {
        this._userOnMessage = handler;
    }

    connect() {
        this._manualClose = false;
        this._createWebsocket();
    }

    send(msg) {
        if (!this.isOpen) return;
        if (msg === Object(msg)) {
            this._ws.send(JSON.stringify(msg));
        } else if (typeof msg === "string") {
            this._ws.send(msg);
        }
    }

    close(code, reason) {
        if (this.isOpen) {
            this._manualClose = true;
            this._ws.close(code, reason);
        }
    }
}