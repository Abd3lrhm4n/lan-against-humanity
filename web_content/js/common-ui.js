(function () {
    window.mobileCheck = function() {
        var check = false;
        (function(a){if(/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a)||/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0,4))) check = true;})(navigator.userAgent||navigator.vendor||window.opera);
        return check;
    };

    setLpIgnore = function() {
        const inputElements = document.getElementsByTagName("input");
        for (e of inputElements) {
            e.setAttribute("data-lpignore", "true");
        }
    }

    showModal = function (modalName) {
        let e = document.getElementById(modalName);
        if (e) {
            e.classList.remove("hidden");
        }
    }

    hideModal = function (modalName) {
        let e = document.getElementById(modalName);
        if (e) {
            e.classList.add("hidden");
        }
    }

    setEnabled = function(elementName, enabled) {
        if (enabled) {
            enable(elementName);
        } else {
            disable(elementName);
        }
    }

    disable = function(elementName) {
        document.getElementById(elementName).classList.add("disabled");
    }

    enable = function(elementName) {
        document.getElementById(elementName).classList.remove("disabled");
    }

    let onTrackScroll = function(element) {
        const LAH_SCROLL_UP = "lah-scroll-up";
        const LAH_SCROLL_DOWN = "lah-scroll-down";
        const LAH_SCROLL_LEFT = "lah-scroll-left";
        const LAH_SCROLL_RIGHT = "lah-scroll-right";

        // Vertical scrolling
        let sy = element.scrollTop;
        let ly = element.scrollHeight - element.clientHeight;
        if (ly <= 0) {
            element.setClass(LAH_SCROLL_UP, false);
            element.setClass(LAH_SCROLL_DOWN, false);
        } else {
            element.setClass(LAH_SCROLL_DOWN, sy < ly);
            element.setClass(LAH_SCROLL_UP, sy > 0);
        }
        // Horizontal scrolling
        let sx = element.scrollLeft;
        let lx = element.scrollWidth - element.clientWidth;
        if (lx <= 0) {
            element.setClass(LAH_SCROLL_LEFT, false);
            element.setClass(LAH_SCROLL_RIGHT, false);
        } else {
            element.setClass(LAH_SCROLL_RIGHT, sx < lx);
            element.setClass(LAH_SCROLL_LEFT, sx > 0);
        }
    }

    Element.prototype.isScrollTrackingEnabled = false;

    Element.prototype.updateScrollTracking = function() {
        let e = this;
        if (e.isScrollTrackingEnabled) {
            onTrackScroll(e);
        }
    }

    Element.prototype.trackScroll = function() { 
        let element = this;
        let handler = () => onTrackScroll(element);
        element.addEventListener("scroll", handler);
        element.addEventListener("resize", handler);
        element.addEventListener("change", handler);
        element.addEventListener("loadmetadata", handler);
        onTrackScroll(element);
        element.isScrollTrackingEnabled = true;
    }

    Element.prototype.toggleClass = function(className) {
        if (this.classList.contains(className)) {
            this.classList.remove(className);
        } else {
            this.classList.add(className);
        }
    }

    Element.prototype.setClass = function(className, enabled) {
        if (enabled) {
            this.classList.add(className);
        } else {
            this.classList.remove(className);
        }
    }

    Element.prototype.killChildren = function(query = null) {
        if (query) {
            let child = this.querySelector(query);
            while(child) {
                this.removeChild(child);
                child = this.querySelector(query);
            }
        } else {
            while(this.firstChild) {
                this.removeChild(this.firstChild);
            }
        }
    }

    Element.prototype.setVisible = function(isVisible) {
        if (isVisible) {
            this.classList.remove("hidden");
        } else {
            this.classList.add("hidden");
        }
    }

    Element.prototype.setVisibleMobile = function(isVisible) {
        if (isVisible) {
            this.classList.remove("mobile-hidden");
        } else {
            this.classList.add("mobile-hidden");
        }
    }

    // Register modal close buttons
    const modals = document.getElementsByClassName("modal");
    for (let modal of modals) {
        let btnClose = modal.querySelector(".modal-close");
        let modalName = modal.id;
        if (btnClose) {
            btnClose.addEventListener("click", () => hideModal(modalName));
        }
    }

    // Remove preload class from body
    document.body.classList.remove("preload");
})();