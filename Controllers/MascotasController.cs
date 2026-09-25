using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // "environment" se mantiene SOLO para poder correr MigrarFotosASupabase
    // una vez con las fotos viejas. Una vez migradas, puedes borrar ese
    // método y quitar IWebHostEnvironment de aquí.
    [Authorize]
    public class MascotasController(
        ApplicationDbContext context,
        SupabaseStorageService storage,
        IWebHostEnvironment environment) : Controller
    {
        // ---------- LISTADO (con filtros, orden y paginación) ----------
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            TipoMascota? tipo, string? busqueda, SexoMascota? sexo,
            string? orden = "recientes", int pagina = 1)
        {
            const int tamanoPagina = 8; // cuántas tarjetas se muestran por página

            var query = context.Mascotas.AsNoTracking().Where(m => m.Disponible);

            if (tipo.HasValue)
                query = query.Where(m => m.Tipo == tipo.Value);

            if (!string.IsNullOrWhiteSpace(busqueda))
                // EF.Functions.ILike (en vez de .Contains) es la búsqueda "sin
                // importar mayúsculas/minúsculas" de PostgreSQL. Antes, buscar
                // "max" no encontraba a "Maximiliano" porque .Contains() SÍ
                // distingue mayúsculas en PostgreSQL (a diferencia de SQL
                // Server, donde por defecto no importa). Los "%" alrededor
                // significan "que contenga este texto en cualquier parte".
                query = query.Where(m => EF.Functions.ILike(m.Nombre, $"%{busqueda}%"));

            if (sexo.HasValue)
                query = query.Where(m => m.Sexo == sexo.Value);

            // Orden: por defecto las más recientes primero.
            query = orden switch
            {
                "antiguas" => query.OrderBy(m => m.FechaIngreso),
                "nombre" => query.OrderBy(m => m.Nombre),
                _ => query.OrderByDescending(m => m.FechaIngreso), // "recientes"
            };

            // Contamos el total ANTES de aplicar Skip/Take, para saber
            // cuántas páginas existen en total.
            var totalMascotas = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalMascotas / (double)tamanoPagina);

            // Si alguien pide una página fuera de rango (por ejemplo
            // escribiendo la URL a mano), la acotamos a un valor válido.
            if (pagina < 1) pagina = 1;
            if (totalPaginas > 0 && pagina > totalPaginas) pagina = totalPaginas;

            var mascotas = await query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            ViewBag.TipoSeleccionado = tipo;
            ViewBag.Busqueda = busqueda;
            ViewBag.SexoSeleccionado = sexo;
            ViewBag.OrdenSeleccionado = orden;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalMascotas = totalMascotas;

            // Mascotas destacadas del Hero: las 5 disponibles más recientes,
            // sin importar los filtros que se estén usando en ese momento
            // (es una consulta aparte, chica y rápida). La vista las va
            // rotando cada 5 segundos en la tarjeta de la derecha.
            ViewBag.MascotasDestacadas = await context.Mascotas.AsNoTracking()
                .Where(m => m.Disponible)
                .OrderByDescending(m => m.FechaIngreso)
                .Take(5)
                .ToListAsync();

            return View(mascotas);
        }

        // ---------- DETALLE ----------
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var mascota = await context.Mascotas.FindAsync(id);
            if (mascota == null) return NotFound();
            return View(mascota);
        }

        // ---------- CREAR ----------
        // Esta versión (sin [HttpPost]) es la que se usa cuando el
        // navegador ABRE la página del formulario vacío (petición GET).
        [Authorize(Roles = "Zoonosis")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Create(Mascota mascota)
        {
            ModelState.Remove(nameof(Mascota.FotoArchivo));
            ModelState.Remove(nameof(Mascota.FotoUrl));

            if (!ModelState.IsValid) return View(mascota);

            // Si no se marcó ningún checkbox de salud, el formulario no manda
            // el campo "EstadosSalud" y el model binder lo deja en null.
            mascota.EstadosSalud ??= new List<string>();

            if (mascota.FotoArchivo != null)
                mascota.FotoUrl = await GuardarFoto(mascota.FotoArchivo);

            context.Mascotas.Add(mascota);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ---------- EDITAR ----------
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Edit(int id)
        {
            var mascota = await context.Mascotas.FindAsync(id);
            if (mascota == null) return NotFound();
            return View(mascota);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Edit(int id, Mascota mascota)
        {
            if (id != mascota.Id) return NotFound();

            ModelState.Remove(nameof(Mascota.FotoArchivo));
            ModelState.Remove(nameof(Mascota.FotoUrl));

            if (!ModelState.IsValid) return View(mascota);

            mascota.EstadosSalud ??= new List<string>();

            var mascotaExistente = await context.Mascotas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascotaExistente == null) return NotFound();

            mascota.FotoUrl = mascotaExistente.FotoUrl;
            if (mascota.FotoArchivo != null)
                mascota.FotoUrl = await GuardarFoto(mascota.FotoArchivo);

            mascota.FechaIngreso = mascotaExistente.FechaIngreso;
            mascota.FechaActualizacion = DateTime.UtcNow;

            context.Mascotas.Update(mascota);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ---------- ELIMINAR ----------
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Delete(int id)
        {
            var mascota = await context.Mascotas.FindAsync(id);
            if (mascota == null) return NotFound();
            return View(mascota);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mascota = await context.Mascotas.FindAsync(id);
            if (mascota != null)
            {
                context.Mascotas.Remove(mascota);
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- PANEL DE ADMINISTRACIÓN (solo Zoonosis) ----------
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Administrar()
        {
            var mascotas = await context.Mascotas
                .AsNoTracking()
                .OrderByDescending(m => m.FechaIngreso)
                .ToListAsync();

            return View(mascotas);
        }

        // ---------- MIGRACIÓN DE FOTOS VIEJAS (uso único) ----------
        // Sube a Supabase las fotos que quedaron guardadas localmente en
        // wwwroot/uploads/mascotas antes de este cambio. Entra una vez a
        // /Mascotas/MigrarFotosASupabase logueado como Zoonosis, y después
        // puedes borrar este método (y el parámetro "environment" del
        // constructor, si ya no lo usas en ningún otro lado).
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> MigrarFotosASupabase()
        {
            var mascotas = await context.Mascotas
                .Where(m => m.FotoUrl != null && m.FotoUrl.StartsWith("/uploads/"))
                .ToListAsync();

            var migradas = 0;

            foreach (var mascota in mascotas)
            {
                var rutaFisica = Path.Combine(
                    environment.WebRootPath,
                    mascota.FotoUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!System.IO.File.Exists(rutaFisica))
                    continue; // el archivo ya no existe, lo saltamos

                await using var stream = System.IO.File.OpenRead(rutaFisica);
                var extension = Path.GetExtension(rutaFisica);
                var contentType = extension.ToLower() switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                // Envolvemos el stream en un FormFile "falso" para poder
                // reutilizar el mismo SubirArchivoAsync que usa el resto
                // de la app, sin duplicar código de subida.
                var formFile = new FormFile(stream, 0, stream.Length, "foto", Path.GetFileName(rutaFisica))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = contentType
                };

                var nuevaUrl = await storage.SubirArchivoAsync(formFile, "mascotas", "fotos");
                mascota.FotoUrl = nuevaUrl;
                migradas++;
            }

            await context.SaveChangesAsync();

            return Content($"Migración completa. {migradas} de {mascotas.Count} fotos migradas a Supabase.");
        }

        // ---------- AUXILIAR ----------
        // Ahora sube la foto a Supabase Storage (bucket "mascotas") en vez
        // de guardarla en el disco local, y devuelve la URL pública.
        private async Task<string> GuardarFoto(IFormFile foto)
        {
            return await storage.SubirArchivoAsync(foto, "mascotas", "fotos");
        }
    }
}