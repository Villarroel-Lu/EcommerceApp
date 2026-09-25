/**
 * Service Worker de AdoptaPatitas.
 *
 * Qué SÍ hace:
 *  - Permite "instalar" el sitio como app (junto con manifest.json).
 *  - Guarda en caché solo archivos que casi no cambian (CSS, JS, logo,
 *    Bootstrap/jQuery) para que carguen más rápido en visitas futuras.
 *  - Si no hay internet y se intenta abrir una página, muestra
 *    offline.html en vez del error feo del navegador.
 *
 * Qué a propósito NO hace (y por qué):
 *  - NO guarda en caché el catálogo, "Mis solicitudes" ni ninguna
 *    página con datos reales. Esas SIEMPRE se piden a internet. Si se
 *    guardaran, alguien podría ver una mascota como "disponible"
 *    cuando ya fue adoptada, o tener problemas para iniciar sesión por
 *    una copia vieja de la página.
 *
 * "CACHE_VERSION" hay que subirlo (v2, v3...) cada vez que se cambie
 * esta lista, para que los navegadores que ya lo instalaron bajen la
 * versión nueva en vez de quedarse con los archivos viejos guardados.
 */
var CACHE_VERSION = 'adoptapatitas-v1';

// Archivos que se guardan en caché al instalar. Se listan SIN el "?v=..."
// que agrega asp-append-version (ese número cambia solo); al leer la
// caché se ignora la parte de la URL después del "?" (ver más abajo,
// "ignoreSearch: true"), así igual encuentra el archivo aunque el hash
// haya cambiado.
var ARCHIVOS_EN_CACHE = [
    '/offline.html',
    '/css/adoptapatitas.css',
    '/css/site.css',
    '/js/site.js',
    '/js/asistente-voz.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/images/logo-adoptapatitas-nav.png',
    '/images/icon-192.png',
    '/favicon.ico'
];

// ---------- Instalar: descarga y guarda los archivos de arriba ----------
self.addEventListener('install', function (evento) {
    evento.waitUntil(
        caches.open(CACHE_VERSION).then(function (cache) {
            // {cache: 'reload'} evita que el navegador use SU PROPIA
            // caché HTTP normal al bajar estos archivos por primera vez.
            return Promise.all(
                ARCHIVOS_EN_CACHE.map(function (url) {
                    return cache.add(new Request(url, { cache: 'reload' })).catch(function () {
                        // Si un archivo puntual falla (por ejemplo, no existe
                        // en este entorno), no se cae toda la instalación.
                    });
                })
            );
        })
    );
    self.skipWaiting();
});

// ---------- Activar: borra cachés de versiones anteriores ----------
self.addEventListener('activate', function (evento) {
    evento.waitUntil(
        caches.keys().then(function (nombres) {
            return Promise.all(
                nombres
                    .filter(function (nombre) { return nombre !== CACHE_VERSION; })
                    .map(function (nombre) { return caches.delete(nombre); })
            );
        })
    );
    self.clients.claim();
});

// ---------- Responder a las peticiones ----------
self.addEventListener('fetch', function (evento) {
    var peticion = evento.request;

    // Solo se interviene en peticiones GET del propio sitio. Todo lo
    // demás (POST de formularios, login, llamadas a Supabase, fuentes de
    // Google, etc.) sigue su camino normal, sin pasar por la caché.
    if (peticion.method !== 'GET' || new URL(peticion.url).origin !== self.location.origin) {
        return;
    }

    // Navegación (abrir una página completa, como /Mascotas o /Solicitudes/MisSolicitudes):
    // SIEMPRE se pide a internet primero. Solo si eso falla (sin
    // conexión) se muestra la página de "sin conexión".
    if (peticion.mode === 'navigate') {
        evento.respondWith(
            fetch(peticion).catch(function () {
                return caches.match('/offline.html');
            })
        );
        return;
    }

    // Archivos estáticos conocidos (CSS, JS, imágenes de la lista de
    // arriba): primero se busca en caché (rápido), y si no está, se pide
    // a internet. "ignoreSearch: true" hace que encuentre el archivo
    // aunque la URL real tenga "?v=algúnHash" agregado.
    evento.respondWith(
        caches.match(peticion, { ignoreSearch: true }).then(function (enCache) {
            return enCache || fetch(peticion);
        })
    );
});