// Development service worker — passthrough only, sem cache.
// Em produção o build substitui por service-worker.published.js.
self.addEventListener('fetch', () => {});
