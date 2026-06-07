(function () {
    'use strict';

    let _canvas = null;
    let _ctx = null;
    let _image = null;
    let _offsetX = 0;
    let _offsetY = 0;
    let _scale = 1;
    let _dragging = false;
    let _lastX = 0;
    let _lastY = 0;
    let _lastPinchDist = null;

    function getRadius() {
        return Math.min(_canvas.width, _canvas.height) / 2 - 10;
    }

    function draw() {
        if (!_canvas || !_image) return;
        const r = getRadius();
        const cx = _canvas.width / 2;
        const cy = _canvas.height / 2;

        _ctx.clearRect(0, 0, _canvas.width, _canvas.height);

        _ctx.fillStyle = 'rgba(0,0,0,0.6)';
        _ctx.fillRect(0, 0, _canvas.width, _canvas.height);

        _ctx.save();
        _ctx.beginPath();
        _ctx.arc(cx, cy, r, 0, Math.PI * 2);
        _ctx.clip();
        _ctx.drawImage(_image, _offsetX, _offsetY, _image.width * _scale, _image.height * _scale);
        _ctx.restore();

        _ctx.strokeStyle = 'rgba(255,255,255,0.85)';
        _ctx.lineWidth = 2;
        _ctx.beginPath();
        _ctx.arc(cx, cy, r, 0, Math.PI * 2);
        _ctx.stroke();
    }

    function clamp() {
        if (!_canvas || !_image) return;
        const r = getRadius();
        const cx = _canvas.width / 2;
        const cy = _canvas.height / 2;
        const iw = _image.width * _scale;
        const ih = _image.height * _scale;
        _offsetX = Math.min(cx - r, Math.max(cx + r - iw, _offsetX));
        _offsetY = Math.min(cy - r, Math.max(cy + r - ih, _offsetY));
    }

    function applyZoom(factor, pivotX, pivotY) {
        if (!_image || !_canvas) return;
        const r = getRadius();
        const minScale = Math.max((r * 2) / _image.width, (r * 2) / _image.height);
        const newScale = Math.max(minScale, _scale * factor);
        const ratio = newScale / _scale;
        _offsetX = pivotX - (pivotX - _offsetX) * ratio;
        _offsetY = pivotY - (pivotY - _offsetY) * ratio;
        _scale = newScale;
        clamp();
        draw();
    }

    function canvasPoint(clientX, clientY) {
        const rect = _canvas.getBoundingClientRect();
        return {
            x: (clientX - rect.left) * (_canvas.width / rect.width),
            y: (clientY - rect.top) * (_canvas.height / rect.height)
        };
    }

    function onMouseDown(e) {
        _dragging = true;
        _lastX = e.clientX;
        _lastY = e.clientY;
    }

    function onMouseMove(e) {
        if (!_dragging) return;
        _offsetX += e.clientX - _lastX;
        _offsetY += e.clientY - _lastY;
        _lastX = e.clientX;
        _lastY = e.clientY;
        clamp();
        draw();
    }

    function onMouseUp() { _dragging = false; }

    function onWheel(e) {
        e.preventDefault();
        const pt = canvasPoint(e.clientX, e.clientY);
        applyZoom(e.deltaY > 0 ? 0.9 : 1.1, pt.x, pt.y);
    }

    function pinchDist(touches) {
        const dx = touches[0].clientX - touches[1].clientX;
        const dy = touches[0].clientY - touches[1].clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }

    function onTouchStart(e) {
        e.preventDefault();
        if (e.touches.length === 1) {
            _dragging = true;
            _lastX = e.touches[0].clientX;
            _lastY = e.touches[0].clientY;
            _lastPinchDist = null;
        } else if (e.touches.length === 2) {
            _dragging = false;
            _lastPinchDist = pinchDist(e.touches);
        }
    }

    function onTouchMove(e) {
        e.preventDefault();
        if (e.touches.length === 1 && _dragging) {
            _offsetX += e.touches[0].clientX - _lastX;
            _offsetY += e.touches[0].clientY - _lastY;
            _lastX = e.touches[0].clientX;
            _lastY = e.touches[0].clientY;
            clamp();
            draw();
        } else if (e.touches.length === 2 && _lastPinchDist !== null) {
            const dist = pinchDist(e.touches);
            const midX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
            const midY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
            const pt = canvasPoint(midX, midY);
            applyZoom(dist / _lastPinchDist, pt.x, pt.y);
            _lastPinchDist = dist;
        }
    }

    function onTouchEnd(e) {
        if (e.touches.length === 0) {
            _dragging = false;
            _lastPinchDist = null;
        }
    }

    function detach() {
        if (!_canvas) return;
        _canvas.removeEventListener('mousedown', onMouseDown);
        _canvas.removeEventListener('mousemove', onMouseMove);
        _canvas.removeEventListener('mouseup', onMouseUp);
        _canvas.removeEventListener('mouseleave', onMouseUp);
        _canvas.removeEventListener('wheel', onWheel);
        _canvas.removeEventListener('touchstart', onTouchStart);
        _canvas.removeEventListener('touchmove', onTouchMove);
        _canvas.removeEventListener('touchend', onTouchEnd);
    }

    window.fotoCropper = {
        init: function (canvas, imageSrc) {
            detach();
            _canvas = canvas;
            _ctx = canvas.getContext('2d');
            _image = null;
            _dragging = false;
            _lastPinchDist = null;

            const img = new Image();
            img.onload = function () {
                _image = img;
                const r = getRadius();
                const minScale = Math.max((r * 2) / img.width, (r * 2) / img.height);
                _scale = minScale;
                _offsetX = canvas.width / 2 - (img.width * minScale) / 2;
                _offsetY = canvas.height / 2 - (img.height * minScale) / 2;
                clamp();
                draw();
            };
            img.src = imageSrc;

            canvas.style.cursor = 'grab';
            canvas.addEventListener('mousedown', onMouseDown);
            canvas.addEventListener('mousemove', onMouseMove);
            canvas.addEventListener('mouseup', onMouseUp);
            canvas.addEventListener('mouseleave', onMouseUp);
            canvas.addEventListener('wheel', onWheel, { passive: false });
            canvas.addEventListener('touchstart', onTouchStart, { passive: false });
            canvas.addEventListener('touchmove', onTouchMove, { passive: false });
            canvas.addEventListener('touchend', onTouchEnd);
        },

        getCroppedDataUrl: function () {
            if (!_image || !_canvas) return null;
            const r = getRadius();
            const cx = _canvas.width / 2;
            const cy = _canvas.height / 2;
            const size = Math.round(r * 2);

            const out = document.createElement('canvas');
            out.width = size;
            out.height = size;
            const outCtx = out.getContext('2d');

            outCtx.beginPath();
            outCtx.arc(r, r, r, 0, Math.PI * 2);
            outCtx.clip();

            outCtx.drawImage(
                _image,
                (cx - r - _offsetX) / _scale,
                (cy - r - _offsetY) / _scale,
                size / _scale,
                size / _scale,
                0, 0, size, size
            );

            return out.toDataURL('image/png');
        }
    };
})();
