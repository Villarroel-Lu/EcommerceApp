using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // Constructor primario: context (base de datos), userManager (para
    // saber quién está logueado y traer sus datos básicos) y storage
    // (para subir las fotos del carnet a Supabase) quedan disponibles
    // en toda la clase.
    [Authorize]
    public class SolicitudesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SupabaseStorageService storage) : Controller
    {
        // ---------- CREAR SOLICITUD (solo Adoptante) ----------

        // GET: muestra el formulario vacío (con Nombres/Apellidos/Email
        // pre-llenados desde la cuenta, para no hacer escribir de nuevo
        // lo que ya sabemos).
        [Authorize(Roles = "Adoptante")]
        public async Task<IActionResult> Crear(int mascotaId)
        {
            var mascota = await context.Mascotas.FindAsync(mascotaId);
            if (mascota == null) return NotFound();

            var usuario = await userManager.GetUserAsync(User);
            if (usuario == null) return RedirectToAction("Login", "Account");

            // Evita que la misma persona duplique el pedido para la
            // misma mascota mientras ya tiene una solicitud activa.
            var yaExiste = await context.Solicitudes.AnyAsync(s =>
                s.MascotaId == mascotaId &&
                s.ApplicationUserId == usuario.Id &&
                (s.Estado == EstadoSolicitud.Pendiente || s.Estado == EstadoSolicitud.Aprobada));

            if (yaExiste)
            {
                TempData["Mensaje"] = "Ya tienes una solicitud activa para esta mascota.";
                return RedirectToAction(nameof(MisSolicitudes));
            }

            var solicitud = new SolicitudAdopcion
            {
                MascotaId = mascota.Id,
                Mascota = mascota,
                Nombres = usuario.Nombre ?? string.Empty,
                Apellidos = usuario.Apellido ?? string.Empty,
                Email = usuario.Email ?? string.Empty
                // Edad, Telefono, DondeVive, NumeroCarnet, fotos, etc. quedan
                // vacíos a propósito: el registro ya no los captura, así
                // que la persona los completa aquí, la primera vez que
                // solicita adoptar.
            };

            return View(solicitud);
        }

        // POST: procesa el formulario, sube las fotos y guarda la solicitud.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Adoptante")]
        public async Task<IActionResult> Crear(SolicitudAdopcion solicitud)
        {
            // Estos campos no se validan como parte del modelo: Mascota es
            // una relación que se carga aparte, y las Url las llena el
            // propio servidor después de subir el archivo, no la persona.
            ModelState.Remove(nameof(SolicitudAdopcion.Mascota));
            ModelState.Remove(nameof(SolicitudAdopcion.ApplicationUserId));
            ModelState.Remove(nameof(SolicitudAdopcion.FotoCarnetAnversoUrl));
            ModelState.Remove(nameof(SolicitudAdopcion.FotoCarnetReversoUrl));

            var mascota = await context.Mascotas.FindAsync(solicitud.MascotaId);
            if (mascota == null) return NotFound();

            var usuario = await userManager.GetUserAsync(User);
            if (usuario == null) return RedirectToAction("Login", "Account");

            // El motivo es OBLIGATORIO: Zoonosis evalúa esta respuesta para decidir
            // la adopción. Se valida aquí (y no con [Required] en el modelo) a
            // propósito: cambiar el modelo obligaría a modificar la base de datos
            // (migración) y a arreglar las solicitudes viejas que no tienen motivo.
            // El formulario también lo exige en el navegador; esto cubre el caso de
            // que alguien se salte el navegador.
            solicitud.Motivo = solicitud.Motivo?.Trim();
            if (string.IsNullOrWhiteSpace(solicitud.Motivo))
                ModelState.AddModelError(nameof(SolicitudAdopcion.Motivo),
                    "Cuéntanos por qué quieres adoptar: Zoonosis evaluará tu respuesta.");
            else if (solicitud.Motivo.Length < 20)
                ModelState.AddModelError(nameof(SolicitudAdopcion.Motivo),
                    "Cuéntanos un poco más (mínimo 20 caracteres).");

            if (!ModelState.IsValid)
            {
                // Si algo falló (por ejemplo, no subió las fotos del
                // carnet), volvemos a mostrar el formulario con los
                // errores. Hay que volver a cargar Mascota porque el
                // formulario no la manda completa, solo el Id.
                solicitud.Mascota = mascota;
                return View(solicitud);
            }

            solicitud.ApplicationUserId = usuario.Id;
            solicitud.Estado = EstadoSolicitud.Pendiente;
            solicitud.FechaSolicitud = DateTime.UtcNow;

            // Las fotos son obligatorias ahora (el [Required] del modelo ya
            // lo garantizó arriba), así que siempre las subimos.
            solicitud.FotoCarnetAnversoUrl = await storage.SubirArchivoAsync(
                solicitud.FotoCarnetAnversoArchivo!, "carnets", "solicitud-anverso");

            solicitud.FotoCarnetReversoUrl = await storage.SubirArchivoAsync(
                solicitud.FotoCarnetReversoArchivo!, "carnets", "solicitud-reverso");

            context.Solicitudes.Add(solicitud);
            await context.SaveChangesAsync();

            TempData["Mensaje"] = $"¡Tu solicitud para adoptar a {mascota.Nombre} fue enviada y está en revisión!";
            return RedirectToAction(nameof(MisSolicitudes));
        }

        // ---------- MIS SOLICITUDES (Adoptante) ----------
        // Esta pantalla ES la "notificación": como no mandamos correos
        // todavía, aquí es donde el adoptante ve si le aprobaron o
        // rechazaron, y por qué.
        [Authorize(Roles = "Adoptante")]
        public async Task<IActionResult> MisSolicitudes()
        {
            var usuarioId = userManager.GetUserId(User);

            var solicitudes = await context.Solicitudes
                .Include(s => s.Mascota) // trae los datos de la mascota junto con la solicitud
                .Where(s => s.ApplicationUserId == usuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudes);
        }

        // ---------- REVISAR SOLICITUDES (Zoonosis) ----------

        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Revisar()
        {
            var solicitudes = await context.Solicitudes
                .Include(s => s.Mascota)
                .OrderBy(s => s.Estado == EstadoSolicitud.Pendiente ? 0 : 1) // pendientes arriba
                .ThenByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await context.Solicitudes.Include(s => s.Mascota).FirstOrDefaultAsync(s => s.Id == id);
            if (solicitud == null) return NotFound();

            solicitud.Estado = EstadoSolicitud.Aprobada;
            solicitud.FechaResolucion = DateTime.UtcNow;

            // La mascota se marca como no disponible: deja de verse en el
            // catálogo público mientras se hace la entrega en persona.
            solicitud.Mascota.Disponible = false;

            // Cualquier OTRA solicitud pendiente para esta misma mascota
            // ya no aplica (se la llevó esta persona), así que las
            // rechazamos automáticamente en vez de dejarlas colgadas.
            var otrasPendientes = await context.Solicitudes
                .Where(s => s.MascotaId == solicitud.MascotaId
                         && s.Id != solicitud.Id
                         && s.Estado == EstadoSolicitud.Pendiente)
                .ToListAsync();

            foreach (var otra in otrasPendientes)
            {
                otra.Estado = EstadoSolicitud.Rechazada;
                otra.Observacion = "La mascota ya fue adoptada por otra persona.";
                otra.FechaResolucion = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Revisar));
        }

        // GET: formulario para escribir el motivo del rechazo
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await context.Solicitudes.Include(s => s.Mascota).FirstOrDefaultAsync(s => s.Id == id);
            if (solicitud == null) return NotFound();
            return View(solicitud);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Zoonosis")]
        public async Task<IActionResult> Rechazar(int id, string observacion)
        {
            var solicitud = await context.Solicitudes.FirstOrDefaultAsync(s => s.Id == id);
            if (solicitud == null) return NotFound();

            solicitud.Estado = EstadoSolicitud.Rechazada;
            solicitud.Observacion = observacion;
            solicitud.FechaResolucion = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Revisar));
        }
    }
}