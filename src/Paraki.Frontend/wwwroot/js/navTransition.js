window.setCssVar = function (name, value) {
    document.documentElement.style.setProperty(name, value);
};

window.matchesMedia = function (query) {
    return window.matchMedia(query).matches;
};

window.navTransition = (function () {
    var _order = { mapa: 0, lista: 1, add: 2, perfil: 3 };
    var _safetyTimer = null;

    return {
        prepare: function (fromId, toId) {
            var from = _order[fromId] ?? -1;
            var to   = _order[toId]   ?? -1;
            if (from < 0 || to < 0 || from === to) return;

            var isPC = window.matchMedia('(min-width: 900px)').matches;
            var dir = isPC
                ? (to > from ? 'down' : 'up')
                : (to > from ? 'rtl'  : 'ltr');
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
            var html = document.documentElement;
            var dir  = html.getAttribute('data-nav-dir');
            var grow = html.hasAttribute('data-nav-grow');

            clearTimeout(_safetyTimer);
            if (dir)  html.removeAttribute('data-nav-dir');
            if (grow) html.removeAttribute('data-nav-grow');

            var el = document.getElementById('page-host');
            if (!el) return;

            // #page-host is persistent across navigation: clear a collapse left
            // behind by the page we navigated away from, so it can't keep the
            // new page clipped to circle(0%) (black screen).
            el.removeAttribute('data-nav-collapse');

            if (!dir && !grow) return;

            // Cancel any running animation so the new one starts fresh.
            el.style.animation = 'none';
            void el.offsetWidth; // trigger reflow
            el.style.animation  = '';

            // 'grow' takes precedence: the page grows back in from the center
            // (the visual inverse of the collapse the previous page played).
            var attr = grow ? 'data-nav-grow-in' : 'data-nav-enter';
            el.setAttribute(attr, grow ? '' : dir);
            el.addEventListener('animationend', function () {
                el.removeAttribute(attr);
            }, { once: true });
        },

        // Collapse the current page into a shrinking circle at the center,
        // then resolve so the caller can navigate. Flags the next page to
        // grow back in. Returns a Promise.
        collapse: function () {
            return new Promise(function (resolve) {
                var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
                var el = document.getElementById('page-host');
                if (reduce || !el) { resolve(); return; }

                document.documentElement.setAttribute('data-nav-grow', '');

                el.style.animation = 'none';
                void el.offsetWidth;
                el.style.animation = '';
                el.setAttribute('data-nav-collapse', '');

                var done = false;
                var finish = function () {
                    if (done) return;
                    done = true;
                    // Flag the next page to grow back in — set only now (right
                    // before the caller navigates) so an intermediate re-render
                    // of this page can't consume it mid-collapse.
                    document.documentElement.setAttribute('data-nav-grow', '');
                    resolve();
                };
                el.addEventListener('animationend', finish, { once: true });
                setTimeout(finish, 500); // safety
            });
        }
    };
})();
