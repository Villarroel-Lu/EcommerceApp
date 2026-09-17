// Agrega esto a wwwroot/js/site.js (o pégalo dentro de un <script> al
// final de _Layout.cshtml, justo antes de cerrar </body>).
// Le da sombra a la navbar solo cuando el usuario baja la página,
// para que se note que "flota" sobre el contenido.
window.addEventListener('scroll', function () {
    var nav = document.getElementById('navbarPrincipal');
    if (!nav) return;

    if (window.scrollY > 10) {
        nav.classList.add('navbar-scrolled');
    } else {
        nav.classList.remove('navbar-scrolled');
    }
});