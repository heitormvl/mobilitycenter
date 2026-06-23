window.imageCompressor = {
    _compress: function (base64, mimeType, maxDim, quality) {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = function () {
                let w = img.width, h = img.height;
                if (w > maxDim || h > maxDim) {
                    if (w >= h) { h = Math.round(h * maxDim / w); w = maxDim; }
                    else        { w = Math.round(w * maxDim / h); h = maxDim; }
                }
                const canvas = document.createElement('canvas');
                canvas.width = w;
                canvas.height = h;
                canvas.getContext('2d').drawImage(img, 0, 0, w, h);
                resolve(canvas.toDataURL('image/jpeg', quality));
            };
            img.onerror = () => resolve(null);
            img.src = 'data:' + mimeType + ';base64,' + base64;
        });
    },

    compress: function (base64, mimeType, maxDim, quality) {
        return this._compress(base64, mimeType, maxDim, quality);
    },

    // Clicks a hidden camera input, waits for the user to take/pick a photo,
    // compresses it, and resolves with JSON { dataUrl, fileName } or null if cancelled.
    captureAndCompress: function (inputId, maxDim, quality) {
        return new Promise((resolve) => {
            const input = document.getElementById(inputId);
            if (!input) { resolve(null); return; }

            const handler = async (e) => {
                input.removeEventListener('change', handler);
                const file = e.target.files && e.target.files[0];
                if (!file) { resolve(null); return; }

                const reader = new FileReader();
                reader.onload = async (ev) => {
                    const base64 = ev.target.result.split(',')[1];
                    const dataUrl = await imageCompressor._compress(base64, file.type || 'image/jpeg', maxDim, quality);
                    input.value = '';
                    resolve(dataUrl ? JSON.stringify({ dataUrl: dataUrl, fileName: file.name }) : null);
                };
                reader.onerror = () => resolve(null);
                reader.readAsDataURL(file);
            };

            input.addEventListener('change', handler);
            input.click();
        });
    }
};
