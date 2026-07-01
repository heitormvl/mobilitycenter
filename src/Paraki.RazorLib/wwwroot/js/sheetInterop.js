window.sheetInterop = (function () {
    let _dotnet = null;
    let _sheet = null;
    let _peekY = 0;
    let _currentY = 0;
    let _isExpanded = false;
    let _tracking = false;
    let _startClientY = 0;
    let _startCurrentY = 0;
    let _movedPx = 0;
    let _lastTouchEnd = 0;

    function applyTransform(y, animate) {
        _currentY = Math.max(0, Math.min(y, _peekY));
        _sheet.style.transition = animate
            ? 'transform 0.38s cubic-bezier(0.32,0.72,0,1)'
            : 'none';
        _sheet.style.transform = `translateY(${_currentY}px)`;
    }

    function snapTo(expanded) {
        _isExpanded = expanded;
        applyTransform(expanded ? 0 : _peekY, true);
        _dotnet.invokeMethodAsync('SetSheetExpanded', expanded);
    }

    function onStart(clientY) {
        _tracking = true;
        _movedPx = 0;
        _startClientY = clientY;
        _startCurrentY = _currentY;
        _sheet.style.transition = 'none';
    }

    function onMove(clientY) {
        if (!_tracking) return;
        const delta = clientY - _startClientY;
        _movedPx = Math.abs(delta);
        applyTransform(_startCurrentY + delta, false);
    }

    function onEnd(clientY) {
        if (!_tracking) return;
        _tracking = false;

        const delta = clientY - _startClientY;

        if (_movedPx < 6) {
            // Tap: toggle between peek and full
            snapTo(!_isExpanded);
            return;
        }

        // Snap to nearest point
        const mid = _peekY / 2;
        if (delta < -40 || _currentY < mid) {
            snapTo(true);
        } else if (delta > 40 || _currentY > mid) {
            snapTo(false);
        } else {
            // Bounce back to current state
            applyTransform(_isExpanded ? 0 : _peekY, true);
        }
    }

    return {
        init(dotnetRef, sheetEl, handleEl, peekTranslateY) {
            _dotnet = dotnetRef;
            _sheet = sheetEl;
            _peekY = peekTranslateY;
            _isExpanded = false;

            applyTransform(_peekY, false);

            handleEl.addEventListener('touchstart', e => {
                onStart(e.touches[0].clientY);
            }, { passive: true });

            document.addEventListener('touchmove', e => {
                if (_tracking && _movedPx > 5) e.preventDefault();
                if (_tracking) onMove(e.touches[0].clientY);
            }, { passive: false });

            document.addEventListener('touchend', e => {
                _lastTouchEnd = Date.now();
                onEnd(e.changedTouches[0].clientY);
            });

            handleEl.addEventListener('mousedown', e => {
                // Ignore synthetic mouse events fired by the browser after touch
                if (Date.now() - _lastTouchEnd < 500) return;
                onStart(e.clientY);
                e.preventDefault();
            });

            document.addEventListener('mousemove', e => {
                if (_tracking) onMove(e.clientY);
            });

            document.addEventListener('mouseup', e => {
                onEnd(e.clientY);
            });
        },

        snapTo(expanded) {
            snapTo(expanded);
        },

        getViewportHeight() {
            return window.innerHeight;
        },

        watchResize(dotnetRef) {
            let t = null;
            this._resizeHandler = () => {
                clearTimeout(t);
                t = setTimeout(() => dotnetRef.invokeMethodAsync('OnWindowResized', window.innerHeight), 150);
            };
            window.addEventListener('resize', this._resizeHandler);
        },

        stopWatchResize() {
            if (this._resizeHandler) {
                window.removeEventListener('resize', this._resizeHandler);
                this._resizeHandler = null;
            }
        }
    };
})();
