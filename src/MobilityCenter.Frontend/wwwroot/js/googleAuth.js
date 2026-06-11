window.googleAuth = {
    _dotNetRef: null,

    signIn: function (clientId, dotNetRef) {
        this._dotNetRef = dotNetRef;

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
                var reason = (notification.getNotDisplayedReason && notification.getNotDisplayedReason())
                    || (notification.getSkippedReason && notification.getSkippedReason())
                    || 'cancelled';
                window.googleAuth._dotNetRef.invokeMethodAsync('HandleGoogleError', reason);
            }
        });
    },

    cancel: function () {
        google.accounts.id.cancel();
    }
};
