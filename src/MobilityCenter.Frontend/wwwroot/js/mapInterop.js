window.mapInterop = {
    _instances: {},

    _tileOptions: {
        attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/attributions">CARTO</a>',
        subdomains: 'abcd',
        maxZoom: 20
    },

    initMap: function (element, mapId) {
        if (this._instances[mapId]) {
            this._instances[mapId].map.remove();
        }

        const map = L.map(element, { zoomControl: false }).setView([-23.5505, -46.6333], 13);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', this._tileOptions).addTo(map);

        const instance = { map, userMarker: null, markers: [], markerLayer: L.layerGroup().addTo(map), dotNetRef: null };
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
        });
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
                instance.map.setView([lat, lng], 15);
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
