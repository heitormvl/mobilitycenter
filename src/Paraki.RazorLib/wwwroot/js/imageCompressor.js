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

    _compressViaObjectUrl: function (blob, maxDim, quality) {
        return new Promise((resolve) => {
            const url = URL.createObjectURL(blob);
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
                URL.revokeObjectURL(url);
                resolve(canvas.toDataURL('image/jpeg', quality));
            };
            img.onerror = () => { URL.revokeObjectURL(url); resolve(null); };
            img.src = url;
        });
    },

    compress: async function (base64, mimeType, maxDim, quality) {
        const normalized = (mimeType || '').toLowerCase();

        if (normalized !== 'image/heic' && normalized !== 'image/heif') {
            return this._compress(base64, normalized || 'image/jpeg', maxDim, quality);
        }

        // For HEIC: try native canvas via object URL first (works on Android 12+)
        const heicBlob = this._base64ToBlob(base64, mimeType);
        const nativeResult = await this._compressViaObjectUrl(heicBlob, maxDim, quality);
        if (nativeResult !== null) return nativeResult;

        // Fall back to heic2any for devices without native HEIC support
        const heic2anyFn = typeof heic2any === 'function'
            ? heic2any
            : (window.heic2any && typeof window.heic2any.default === 'function' ? window.heic2any.default : null);

        if (!heic2anyFn) {
            console.error('[imageCompressor] heic2any not available:', typeof window.heic2any);
            return null;
        }

        try {
            const converted = await heic2anyFn({ blob: heicBlob, toType: 'image/jpeg', quality: quality });
            const jpegBlob = Array.isArray(converted) ? converted[0] : converted;
            const jpegBase64 = await this._blobToBase64(jpegBlob);
            return this._compress(jpegBase64, 'image/jpeg', maxDim, quality);
        } catch (e) {
            console.error('[imageCompressor] heic2any conversion failed:', e);
            return null;
        }
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
