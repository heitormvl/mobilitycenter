// Service worker de produção — caches todos os assets do Blazor WASM para suporte offline.
const cacheNamePrefix = 'paraki-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

const offlineAssetsInclude = [
    /\.dll$/, /\.pdb$/, /\.wasm$/, /\.html$/, /\.js$/,
    /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/,
    /\.blat$/, /\.dat$/
];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

const base = '/';
const baseUrl = new URL(base, self.origin);

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

async function onInstall() {
    console.info('[SW] Install');
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(p => p.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(p => p.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate() {
    console.info('[SW] Activate');
    const keys = await caches.keys();
    await Promise.all(
        keys.filter(k => k.startsWith(cacheNamePrefix) && k !== cacheName)
            .map(k => caches.delete(k))
    );
}

async function onFetch(event) {
    if (event.request.method !== 'GET') return fetch(event.request);

    const isNavigation = event.request.mode === 'navigate';
    const request = isNavigation ? 'index.html' : event.request;

    const cache = await caches.open(cacheName);
    const cached = await cache.match(request);
    return cached ?? fetch(event.request);
}
