(function() {
    let notifyBannerTimeoutToken = null;
    let banner = document.querySelector("#notify-banner");
    let bannerText = document.querySelector("#notify-banner-text");

    togglePlayerList = function() {
        document.querySelector("#player-list").toggleClass("closed");
    }

    toggleMobileNav = function() {
        document.querySelector("#navbar").toggleClass("mobile-hidden");
    }

    toggleFullscreen = function() {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen();
        } else {
            document.exitFullscreen();
        }
    }

    showBannerMessage = function(msg, seconds) {
        bannerText.innerHTML = msg;
        banner.setClass("hidden", false);
        if (notifyBannerTimeoutToken !== null) {
            clearTimeout(notifyBannerTimeoutToken);
        }
        notifyBannerTimeoutToken = setTimeout(() => {
            banner.setClass("hidden", true);
            notifyBannerTimeoutToken = null;
        }, (seconds || 2.5) * 1000);
    }

    if (window.mobileCheck()) {
        let w = Math.max(document.documentElement.clientWidth, window.innerWidth || 0);
        let h = Math.max(document.documentElement.clientHeight, window.innerHeight || 0);
        let bodyElement = document.querySelector("body");
        let htmlElement = document.querySelector("html");
        bodyElement.style.width = w + "px";
        bodyElement.style.height = h + "px";
        htmlElement.style.width = w + "px";
        htmlElement.style.height = h + "px";   
    }

    document.querySelector("#hand-cards-scroll-area").trackScroll();
    document.querySelector("#play-cards-scroll-area").trackScroll();
    document.querySelector("#btn-fullscreen").addEventListener("mousedown", () => {
        toggleFullscreen();
    });
    setLpIgnore();
})();