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

    _base64ToBlob: function (base64, mimeType) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
        return new Blob([bytes], { type: mimeType });
    },

    _blobToBase64: function (blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result.split(',')[1]);
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    },

    compress: async function (base64, mimeType, maxDim, quality) {
        const normalized = (mimeType || '').toLowerCase();
        if (normalized === 'image/heic' || normalized === 'image/heif') {
            try {
                const blob = this._base64ToBlob(base64, mimeType);
                const converted = await heic2any({ blob, toType: 'image/jpeg', quality: quality });
                const jpegBlob = Array.isArray(converted) ? converted[0] : converted;
                const jpegBase64 = await this._blobToBase64(jpegBlob);
                return this._compress(jpegBase64, 'image/jpeg', maxDim, quality);
            } catch {
                return null;
            }
        }
        return this._compress(base64, normalized || 'image/jpeg', maxDim, quality);
    },

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
                    const dataUrl = await imageCompressor.compress(base64, file.type || 'image/jpeg', maxDim, quality);
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
