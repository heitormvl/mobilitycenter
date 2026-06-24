window.mapInterop = {
    _instances: {},
    _navRef: null,

    setNavRef: function (dotNetRef) {
        this._navRef = dotNetRef;
    },

    // Expand the clicked popup into the detail screen, then navigate.
    // lat/lng are persisted so initMap can position the map instantly on return.
    navigateTo: function (ev, path, lat, lng) {
        if (ev) ev.preventDefault();
        if (lat != null && lng != null) {
            try { sessionStorage.setItem('mc-focus-pos', JSON.stringify({ lat, lng })); } catch (_) {}
        }

        const go = () => {
            if (this._navRef) this._navRef.invokeMethodAsync('NavigateTo', path);
        };

        const reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const popup = ev && ev.currentTarget ? ev.currentTarget.closest('.leaflet-popup') : null;
        if (reduce || !popup) { go(); return; }

        const rect = popup.getBoundingClientRect();
        const vw = window.innerWidth;
        const vh = window.innerHeight;

        const ov = document.createElement('div');
        ov.className = 'mc-expand';
        ov.style.left = rect.left + 'px';
        ov.style.top = rect.top + 'px';
        ov.style.width = rect.width + 'px';
        ov.style.height = rect.height + 'px';
        document.body.appendChild(ov);

        // Force reflow so the starting geometry is committed before expanding.
        void ov.offsetWidth;

        ov.style.left = '0px';
        ov.style.top = '0px';
        ov.style.width = vw + 'px';
        ov.style.height = vh + 'px';
        ov.style.borderRadius = '0px';

        // Swap the page underneath while the overlay still covers it, then
        // fade the overlay out to reveal the freshly-rendered detail screen.
        setTimeout(() => {
            go();
            requestAnimationFrame(() => ov.classList.add('mc-expand--fade'));
            setTimeout(() => ov.remove(), 280);
        }, 320);
    },

    _tileOptions: {
        attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/attributions">CARTO</a>',
        subdomains: 'abcd',
        maxZoom: 20
    },

    initMap: function (element, mapId) {
        if (this._instances[mapId]) {
            this._instances[mapId].map.remove();
        }

        // If returning from a detail page, jump directly to the cached position
        // so the map never shows the default SP centre.
        const focusId  = new URLSearchParams(location.search).get('focus');
        let initLat = -23.5505, initLng = -46.6333, initZoom = 13;
        let cachedFocus = null;
        if (focusId) {
            try {
                const raw = sessionStorage.getItem('mc-focus-pos');
                if (raw) { cachedFocus = JSON.parse(raw); sessionStorage.removeItem('mc-focus-pos'); }
            } catch (_) {}
            if (cachedFocus) { initLat = cachedFocus.lat; initLng = cachedFocus.lng; initZoom = 16; }
        }

        const map = L.map(element, { zoomControl: false }).setView([initLat, initLng], initZoom);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', this._tileOptions).addTo(map);

        const instance = { map, userMarker: null, markers: [], markersById: {}, pendingFocus: focusId ?? null, focused: false, markerLayer: L.layerGroup().addTo(map), dotNetRef: null };
        this._instances[mapId] = instance;

        this._locateUser(instance);
    },

    // Register a .NET callback fired (debounced) whenever the visible area changes.
    onViewChange: function (mapId, dotNetRef) {
        const inst = this._instances[mapId];
        if (!inst) return;
        inst.dotNetRef = dotNetRef;

        let t = null;
        const notify = () => {
            clearTimeout(t);
            t = setTimeout(() => {
                if (inst.dotNetRef) inst.dotNetRef.invokeMethodAsync('OnViewChangedJs');
            }, 250);
        };
        inst.map.on('moveend zoomend', notify);
    },

    // markers: [{ id, lat, lng, accent, popupHtml }]
    setMarkers: function (mapId, markers) {
        const inst = this._instances[mapId];
        if (!inst) return;

        inst.markerLayer.clearLayers();
        inst.markers = [];
        inst.markersById = {};

        markers.forEach(m => {
            const icon = L.divIcon({
                className: 'mc-pin',
                html: `<div class="mc-pin-dot${m.accent ? ' mc-pin-dot--accent' : ''}"><i class="fa-solid fa-bicycle"></i></div>`,
                iconSize: [34, 34],
                iconAnchor: [17, 34],
                popupAnchor: [0, -32]
            });

            const marker = L.marker([m.lat, m.lng], { icon })
                .bindPopup(m.popupHtml, { closeButton: true, minWidth: 268, maxWidth: 300, className: 'mc-popup' });

            marker.addTo(inst.markerLayer);
            inst.markers.push(marker);
            inst.markersById[m.id] = marker;
        });

        // A focus request may have arrived before the markers existed.
        this._applyFocus(inst);
    },

    // Center on a marker by id and open its popup. Stored as pending if the
    // markers haven't rendered yet (re-applied from setMarkers).
    focusMarker: function (mapId, id) {
        const inst = this._instances[mapId];
        if (!inst) return;
        inst.pendingFocus = id;
        this._applyFocus(inst);
    },

    _applyFocus: function (inst) {
        if (!inst.pendingFocus) return;
        const marker = inst.markersById[inst.pendingFocus];
        if (!marker) return;

        inst.focused = true;
        inst.pendingFocus = null;

        const zoom = 16;
        const latlng = marker.getLatLng();

        // Center the map so the pin sits in the lower half of the viewport,
        // leaving room for the popup above the pin and the search bar at the top.
        // We shift the map center upward by ~140px (in pixel space at zoom 16),
        // which moves the pin proportionally downward on screen.
        const pinPx = inst.map.project(latlng, zoom);
        const centerPx = pinPx.subtract([0, 140]);
        const centerLatLng = inst.map.unproject(centerPx, zoom);

        inst.map.setView(centerLatLng, zoom, { animate: false });
        marker.openPopup();
    },

    getBounds: function (mapId) {
        const inst = this._instances[mapId];
        if (!inst) return null;
        const b = inst.map.getBounds();
        return [b.getSouth(), b.getWest(), b.getNorth(), b.getEast()];
    },

    fitBounds: function (mapId, south, west, north, east) {
        const inst = this._instances[mapId];
        if (inst) inst.map.fitBounds([[south, west], [north, east]], { padding: [50, 50], maxZoom: 16 });
    },

    _locateUser: function (instance) {
        if (!navigator.geolocation) return;

        navigator.geolocation.getCurrentPosition(
            pos => {
                const { latitude: lat, longitude: lng } = pos.coords;
                // Don't steal the view from an active focus request (back-from-detail).
                if (!instance.focused && !instance.pendingFocus) {
                    instance.map.setView([lat, lng], 15);
                }
                this._setUserMarker(instance, lat, lng);
            },
            err => console.warn('Geolocalização indisponível:', err.message)
        );
    },

    _setUserMarker: function (instance, lat, lng) {
        if (instance.userMarker) instance.userMarker.remove();

        instance.userMarker = L.circleMarker([lat, lng], {
            radius: 8,
            fillColor: '#1D6EF5',
            color: 'white',
            weight: 3,
            opacity: 1,
            fillOpacity: 1
        }).addTo(instance.map);
    },

    panToUser: function (mapId) {
        const inst = this._instances[mapId];
        if (!inst) return;

        if (inst.userMarker) {
            inst.map.panTo(inst.userMarker.getLatLng());
            return;
        }

        if (!navigator.geolocation) return;

        navigator.geolocation.getCurrentPosition(pos => {
            const { latitude: lat, longitude: lng } = pos.coords;
            inst.map.setView([lat, lng], 15);
            this._setUserMarker(inst, lat, lng);
        });
    },

    zoomIn: function (mapId) {
        const inst = this._instances[mapId];
        if (inst) inst.map.zoomIn();
    },

    zoomOut: function (mapId) {
        const inst = this._instances[mapId];
        if (inst) inst.map.zoomOut();
    },

    panTo: function (mapId, lat, lng) {
        const inst = this._instances[mapId];
        if (inst) inst.map.setView([lat, lng], 16);
    },

    getCenter: function (mapId) {
        const inst = this._instances[mapId];
        if (!inst) return null;
        const c = inst.map.getCenter();
        return [c.lat, c.lng];
    },

    getUserLocation: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject('Geolocalização não disponível neste navegador');
                return;
            }
            navigator.geolocation.getCurrentPosition(
                pos => resolve([pos.coords.latitude, pos.coords.longitude]),
                err => reject(err.message)
            );
        });
    },

    getCachedLocation: function () {
        try {
            const s = localStorage.getItem('paraki-location');
            return s ? JSON.parse(s) : null;
        } catch (_) { return null; }
    },

    setCachedLocation: function (lat, lng) {
        try { localStorage.setItem('paraki-location', JSON.stringify([lat, lng])); } catch (_) {}
    },

    destroy: function (mapId) {
        const inst = this._instances[mapId];
        if (inst) {
            inst.dotNetRef = null;
            inst.map.off('moveend zoomend');
            inst.map.remove();
            delete this._instances[mapId];
        }
    }
};
