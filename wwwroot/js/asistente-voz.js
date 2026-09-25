/**
 * Asistente de voz de AdoptaPatitas.
 * Escucha lo que dice la persona (Web Speech API del navegador, sin
 * backend, sin IA, sin costo), interpreta la intención comparando
 * palabras clave y lleva a la página que corresponde.
 *
 * Soporte: Chrome, Edge y Brave. En Firefox y la mayoría de Safari no
 * existe SpeechRecognition: en ese caso el botón simplemente no se
 * muestra (nada se rompe).
 *
 * "interpretar" está separado a propósito de la interfaz (botón, panel,
 * micrófono): no toca la página, así que se puede probar solo, en la
 * consola del navegador, con:
 *   AsistenteVoz.interpretar('quiero ver perros')
 */
(function () {

    // ---------------------------------------------------------------
    // DICCIONARIOS (texto sin tildes y en minúscula)
    // ---------------------------------------------------------------

    // Palabras que piden ver perros o gatos.
    var PALABRAS_PERRO = ['perro', 'perros', 'perrito', 'perritos', 'cachorro', 'cachorros'];
    var PALABRAS_GATO = ['gato', 'gatos', 'gatito', 'gatitos', 'michi', 'michis'];

    // Sexo de la mascota.
    var PALABRAS_HEMBRA = ['hembra', 'hembras'];
    var PALABRAS_MACHO = ['macho', 'machos'];

    // Palabras sin significado para la búsqueda por nombre
    // ("quiero buscar a Max" -> solo queda "max").
    var PALABRAS_VACIAS = ['quiero', 'quisiera', 'buscar', 'busca', 'busco', 'buscame', 'muestrame',
        'mostrar', 'muestra', 'ver', 'veo', 'dame', 'necesito', 'ir', 'vamos', 'llevame', 'abrir', 'abre',
        'hay', 'tienes', 'tienen', 'algo', 'alguna', 'algun', 'algunas', 'algunos', 'un', 'una', 'unos',
        'unas', 'el', 'la', 'los', 'las', 'a', 'al', 'de', 'del', 'en', 'para', 'por', 'con', 'que', 'y',
        'o', 'me', 'mi', 'mis', 'hacia', 'favor', 'porfa', 'nombre', 'llamada', 'llamado', 'se',
        'perro', 'perros', 'perrito', 'perritos', 'cachorro', 'cachorros',
        'gato', 'gatos', 'gatito', 'gatitos', 'michi', 'michis',
        'hembra', 'hembras', 'macho', 'machos'];

    // ---------------------------------------------------------------
    // INTERPRETAR: texto dicho -> { url, mensaje } o null
    // ---------------------------------------------------------------

    // Quita tildes y pasa a minúscula, para comparar sin importar cómo
    // el navegador transcribió lo dicho.
    function normalizar(texto) {
        return texto.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    }

    function interpretar(textoOriginal) {
        var texto = normalizar(textoOriginal);
        var palabrasNorm = texto.replace(/[¿?¡!.,]/g, ' ').split(/\s+/).filter(Boolean);
        var tiene = function (lista) {
            return palabrasNorm.some(function (p) { return lista.indexOf(p) !== -1; });
        };

        // 1. Páginas directas (frases completas, se revisan primero).
        if (/\bmis solicitudes\b/.test(texto) || /\bsolicitudes\b/.test(texto))
            return { url: '/Solicitudes/MisSolicitudes', mensaje: 'Abriendo tus solicitudes' };
        if (/\bcomo adoptar\b/.test(texto) || /\badoptar\b/.test(texto) && palabrasNorm.length <= 3)
            return { url: '/Home/Privacy', mensaje: 'Te muestro cómo adoptar' };
        if (/\bnosotros\b/.test(texto) || /\bquienes son\b/.test(texto))
            return { url: '/Home/Nosotros', mensaje: 'Abriendo Nosotros' };
        if (/\biniciar sesion\b/.test(texto) || /\blogin\b/.test(texto))
            return { url: '/Account/Login', mensaje: 'Abriendo inicio de sesión' };
        if (/\bcrear cuenta\b/.test(texto) || /\bregistrarme\b/.test(texto) || /\bregistrar\b/.test(texto))
            return { url: '/Account/Register', mensaje: 'Abriendo registro' };
        if (/\bpanel de administracion\b/.test(texto) || /\badministrar\b/.test(texto))
            return { url: '/Mascotas/Administrar', mensaje: 'Abriendo el panel de administración' };
        if (/\brevisar solicitudes\b/.test(texto))
            return { url: '/Solicitudes/Revisar', mensaje: 'Abriendo solicitudes por revisar' };
        if (/\breportes\b/.test(texto))
            return { url: '/Reportes', mensaje: 'Abriendo reportes' };
        if (/\binicio\b/.test(texto) || /\bmascotas\b/.test(texto) && palabrasNorm.length <= 2)
            return { url: '/Mascotas', mensaje: 'Volviendo al catálogo' };

        // 2. Tipo de mascota (perro / gato), combinado con sexo si lo menciona.
        var esPerro = tiene(PALABRAS_PERRO);
        var esGato = tiene(PALABRAS_GATO);
        if (esPerro || esGato) {
            var params = new URLSearchParams();
            params.set('tipo', esPerro ? 'Perro' : 'Gato');
            if (tiene(PALABRAS_HEMBRA)) params.set('sexo', 'Hembra');
            else if (tiene(PALABRAS_MACHO)) params.set('sexo', 'Macho');

            var etiqueta = (esPerro ? 'perros' : 'gatos');
            return { url: '/Mascotas?' + params.toString(), mensaje: 'Buscando ' + etiqueta };
        }

        // 3. Búsqueda por nombre: si no coincidió nada de arriba pero SÍ
        //    dijo "busca"/"muestrame"/etc., se asume que el resto de la
        //    frase es el nombre de la mascota.
        var pidioBuscar = /\bbusca|buscar|buscame|muestrame|mostrar\b/.test(texto);
        if (pidioBuscar) {
            // Se usa el texto ORIGINAL (con tildes) para no romper nombres
            // como "José" o "Muñeco" al mandarlos a la búsqueda.
            var nombre = textoOriginal
                .replace(/[¿?¡!.,]/g, ' ')
                .split(/\s+/)
                .filter(function (p) { return p && PALABRAS_VACIAS.indexOf(normalizar(p)) === -1; })
                .join(' ')
                .trim();

            if (nombre) {
                return {
                    url: '/Mascotas?busqueda=' + encodeURIComponent(nombre),
                    mensaje: 'Buscando a "' + nombre + '"'
                };
            }
        }

        // 4. Nada coincidió.
        return null;
    }

    // ---------------------------------------------------------------
    // INTERFAZ: botón flotante, panel de mensajes y reconocimiento de voz
    // ---------------------------------------------------------------

    // Frases de ejemplo que se muestran cuando no se entendió nada.
    var EJEMPLOS = [
        'Prueba decir: "ver perros"',
        '"gatos hembra"',
        '"busca a Max"',
        '"mis solicitudes"',
        '"cómo adoptar"'
    ];

    function iniciar() {
        var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

        // Sin soporte del navegador: el botón no se muestra, nada se rompe.
        if (!SpeechRecognition) return;

        var contenedor = document.getElementById('asistenteVoz');
        if (!contenedor) return;

        var boton = contenedor.querySelector('.asistente-voz__btn');
        var panel = contenedor.querySelector('.asistente-voz__panel');
        var estadoEl = contenedor.querySelector('.asistente-voz__estado');
        var dichoEl = contenedor.querySelector('.asistente-voz__dicho');

        var reconocimiento = null;
        var escuchando = false;

        function mostrarPanel(estado, texto) {
            estadoEl.textContent = estado;
            dichoEl.textContent = texto;
            panel.hidden = false;
        }

        function ocultarPanelLuego(ms) {
            window.clearTimeout(contenedor._timeoutOcultar);
            contenedor._timeoutOcultar = window.setTimeout(function () {
                panel.hidden = true;
            }, ms);
        }

        function detener() {
            if (reconocimiento) reconocimiento.abort();
        }

        // Si otro micrófono del sitio empieza a escuchar (por ejemplo, el
        // dictado del motivo de adopción), este se corta solo.
        document.addEventListener('voz:detener', detener);

        function empezarAEscuchar() {
            // Avisa a cualquier otro micrófono activo en la página que se
            // detenga, ANTES de empezar este (para no cortarse a sí mismo).
            document.dispatchEvent(new CustomEvent('voz:detener'));

            reconocimiento = new SpeechRecognition();
            reconocimiento.lang = 'es-BO';
            reconocimiento.interimResults = false;
            reconocimiento.maxAlternatives = 1;

            reconocimiento.onstart = function () {
                escuchando = true;
                contenedor.classList.add('asistente-voz--escuchando');
                mostrarPanel('Te escucho…', 'Di algo como "ver perros" o "busca a Max"');
            };

            reconocimiento.onresult = function (evento) {
                var texto = evento.results[0][0].transcript;
                var resultado = interpretar(texto);

                if (resultado) {
                    mostrarPanel('Entendido', '"' + texto + '" → ' + resultado.mensaje);
                    window.setTimeout(function () {
                        window.location.href = resultado.url;
                    }, 700);
                } else {
                    mostrarPanel('No entendí eso', EJEMPLOS[Math.floor(Math.random() * EJEMPLOS.length)]);
                    ocultarPanelLuego(4000);
                }
            };

            reconocimiento.onerror = function (evento) {
                if (evento.error === 'not-allowed' || evento.error === 'service-not-allowed') {
                    mostrarPanel('Sin acceso', 'No se pudo usar el micrófono');
                } else if (evento.error !== 'aborted') {
                    mostrarPanel('Ups', 'No se pudo escuchar bien, intenta de nuevo');
                }
                ocultarPanelLuego(3500);
            };

            reconocimiento.onend = function () {
                escuchando = false;
                contenedor.classList.remove('asistente-voz--escuchando');
            };

            reconocimiento.start();
        }

        boton.addEventListener('click', function () {
            if (escuchando) {
                detener();
            } else {
                empezarAEscuchar();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', iniciar);
    } else {
        iniciar();
    }

    // Expuesto para poder probar "interpretar" solo, desde la consola.
    window.AsistenteVoz = { interpretar: interpretar };
})();