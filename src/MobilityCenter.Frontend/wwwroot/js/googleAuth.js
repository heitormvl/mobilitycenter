window.googleAuth = {
    _dotNetRef: null,
    _client: null,

    signIn: function (clientId, dotNetRef) {
        this._dotNetRef = dotNetRef;

        // Reinitialize on every call so suppression state is cleared
        google.accounts.id.cancel();

        if (!this._client) {
            this._client = google.accounts.oauth2.initCodeClient({
                client_id: clientId,
                scope: 'openid email profile',
                ux_mode: 'popup',
                callback: function (response) {
                    if (response.error) {
                        var reason = response.error === 'access_denied' ? 'suppressed_by_user' : response.error;
                        window.googleAuth._dotNetRef.invokeMethodAsync('HandleGoogleError', reason);
                        return;
                    }
                    window.googleAuth._dotNetRef.invokeMethodAsync('HandleGoogleError', 'id_token_not_supported');
                }
            });
        }

        // Use One Tap with fallback to popup button
        google.accounts.id.initialize({
            client_id: clientId,
            callback: function (response) {
                window.googleAuth._dotNetRef.invokeMethodAsync('HandleGoogleCredential', response.credential);
            },
            auto_select: false,
            cancel_on_tap_outside: true
        });

        google.accounts.id.prompt(function (notification) {
            if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
                // One Tap was suppressed — fall back to popup window
                window.googleAuth._openPopup(clientId);
            }
        });
    },

    _openPopup: function (clientId) {
        var containerId = '__google_btn_fallback';
        var container = document.getElementById(containerId);
        if (!container) {
            container = document.createElement('div');
            container.id = containerId;
            container.style.cssText = 'position:fixed;opacity:0;pointer-events:none;top:0;left:0;';
            document.body.appendChild(container);
        }
        container.innerHTML = '';

        google.accounts.id.renderButton(container, {
            type: 'standard',
            theme: 'outline',
            size: 'large',
            text: 'signin_with'
        });

        // Click the rendered button — GSI handles the popup internally, no redirect URI needed
        setTimeout(function () {
            var btn = container.querySelector('[role=button], button, div[tabindex]');
            if (btn) btn.click();
        }, 100);
    },

    cancel: function () {
        google.accounts.id.cancel();
    }
};
