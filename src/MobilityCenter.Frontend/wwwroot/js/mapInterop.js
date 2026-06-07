window.mapInterop = {
    _instances: {},

    initMap: function (element, mapId) {
        if (this._instances[mapId]) {
            this._instances[mapId].map.remove();
        }

        const map = L.map(element, { zoomControl: false }).setView([-23.5505, -46.6333], 13);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
            attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        }).addTo(map);

        const instance = { map, userMarker: null };
        this._instances[mapId] = instance;

        this._locateUser(instance);
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

    destroy: function (mapId) {
        const inst = this._instances[mapId];
        if (inst) {
            inst.map.remove();
            delete this._instances[mapId];
        }
    }
};
