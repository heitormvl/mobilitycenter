window.loginAnim = {
    getRect: function (el) {
        const r = el.getBoundingClientRect();
        return { top: r.top, left: r.left, width: r.width, height: r.height };
    },

    runSplit: function (btn, email, pass) {
        const bh = btn.height;

        // Fixed container sitting exactly over the original button
        const wrap = document.createElement('div');
        wrap.style.cssText =
            'position:fixed;top:' + btn.top + 'px;left:' + btn.left + 'px;' +
            'width:' + btn.width + 'px;height:' + bh + 'px;' +
            'pointer-events:none;z-index:9999;overflow:visible;';

        function makeRect() {
            const el = document.createElement('div');
            el.style.cssText =
                'position:absolute;left:0;right:0;top:0;' +
                'height:' + bh + 'px;' +
                'background:var(--bg);border:1.5px solid var(--border);' +
                'border-radius:var(--r-lg);';
            return el;
        }

        const topEl = makeRect();
        const botEl = makeRect();
        wrap.append(topEl, botEl);
        document.body.appendChild(wrap);

        // Delta: how far each rect travels from the button's top-left
        const topDy = email.top  - btn.top;
        const topDh = email.height - bh;
        const botDy = pass.top   - btn.top;
        const botDh = pass.height  - bh;

        const ease = 'cubic-bezier(0.4,0,0.2,1)';
        const dur  = 360;

        topEl.animate([
            { transform: 'translateY(0)',           height: bh + 'px',            opacity: 1 },
            { transform: 'translateY(' + topDy + 'px)', height: (bh + topDh) + 'px', opacity: 1, offset: 0.62 },
            { transform: 'translateY(' + topDy + 'px)', height: (bh + topDh) + 'px', opacity: 0 }
        ], { duration: dur, easing: ease, fill: 'forwards' });

        botEl.animate([
            { transform: 'translateY(0)',           height: bh + 'px',            opacity: 1 },
            { transform: 'translateY(' + botDy + 'px)', height: (bh + botDh) + 'px', opacity: 1, offset: 0.62 },
            { transform: 'translateY(' + botDy + 'px)', height: (bh + botDh) + 'px', opacity: 0 }
        ], { duration: dur + 40, easing: ease, fill: 'forwards' });

        setTimeout(function () { wrap.remove(); }, dur + 200);
    },

    // Reverse: each input rect flies back to the button position
    runMerge: function (email, pass, btn) {
        const ease = 'cubic-bezier(0.4,0,0.2,1)';
        const dur  = 360;

        function makeAndAnimate(fromRect, delay) {
            const el = document.createElement('div');
            el.style.cssText =
                'position:fixed;top:' + fromRect.top + 'px;left:' + fromRect.left + 'px;' +
                'width:' + fromRect.width + 'px;height:' + fromRect.height + 'px;' +
                'background:var(--bg);border:1.5px solid var(--border);' +
                'border-radius:var(--r-lg);pointer-events:none;z-index:9999;';
            document.body.appendChild(el);

            const dy = btn.top    - fromRect.top;
            const dh = btn.height - fromRect.height;

            el.animate([
                { transform: 'translateY(0)',            height: fromRect.height + 'px',        opacity: 1 },
                { transform: 'translateY(' + dy + 'px)', height: (fromRect.height + dh) + 'px', opacity: 1, offset: 0.62 },
                { transform: 'translateY(' + dy + 'px)', height: (fromRect.height + dh) + 'px', opacity: 0 }
            ], { duration: dur, delay: delay, easing: ease, fill: 'forwards' });

            setTimeout(function () { el.remove(); }, delay + dur + 100);
        }

        makeAndAnimate(email, 0);
        makeAndAnimate(pass, 40);
    }
};
