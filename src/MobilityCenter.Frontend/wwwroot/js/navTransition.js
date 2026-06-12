window.navTransition = (function () {
    var _order = { mapa: 0, lista: 1, add: 2, perfil: 3 };
    var _safetyTimer = null;

    return {
        prepare: function (fromId, toId) {
            var from = _order[fromId] ?? -1;
            var to   = _order[toId]   ?? -1;
            if (from < 0 || to < 0 || from === to) return;

            var dir = to > from ? 'rtl' : 'ltr';
            document.documentElement.setAttribute('data-nav-dir', dir);

            // Safety valve: if onPageRender never fires (e.g. target uses a
            // different layout and has no #page-host), clear the attribute so
            // it doesn't leak into the next navigation.
            clearTimeout(_safetyTimer);
            _safetyTimer = setTimeout(function () {
                document.documentElement.removeAttribute('data-nav-dir');
            }, 600);
        },

        onPageRender: function () {
            var dir = document.documentElement.getAttribute('data-nav-dir');
            if (!dir) return;

            clearTimeout(_safetyTimer);
            document.documentElement.removeAttribute('data-nav-dir');

            var el = document.getElementById('page-host');
            if (!el) return;

            // Cancel any running animation so the new one starts fresh.
            el.style.animation = 'none';
            void el.offsetWidth; // trigger reflow
            el.style.animation  = '';

            el.setAttribute('data-nav-enter', dir);
            el.addEventListener('animationend', function () {
                el.removeAttribute('data-nav-enter');
            }, { once: true });
        }
    };
})();
