using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Solo Zoonosis puede ver esta lista — son datos de otras personas.
    [Authorize(Roles = "Zoonosis")]
    public class AdoptantesController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context) : Controller
    {
        // Listado de todas las cuentas con rol "Adoptante". No es un CRUD
        // completo a propósito: crear/editar/borrar cuentas de usuario es
        // responsabilidad de cada persona sobre su propia cuenta (Identity
        // ya se encarga de eso vía registro/login), Zoonosis solo consulta.
        public async Task<IActionResult> Index()
        {
            var adoptantes = await userManager.GetUsersInRoleAsync("Adoptante");

            // Para cada adoptante, contamos cuántas solicitudes ha hecho
            // en total — un dato útil de un vistazo sin entrar al detalle.
            var conteos = await context.Solicitudes
                .GroupBy(s => s.ApplicationUserId)
                .Select(g => new { UsuarioId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.UsuarioId, x => x.Total);

            ViewBag.ConteosSolicitudes = conteos;

            return View(adoptantes.OrderBy(a => a.Nombre).ToList());
        }

        // Detalle de un adoptante: sus datos básicos + todas sus
        // solicitudes (con el estado de cada una).
        public async Task<IActionResult> Details(string id)
        {
            var adoptante = await userManager.FindByIdAsync(id);
            if (adoptante == null) return NotFound();

            var solicitudes = await context.Solicitudes
                .Include(s => s.Mascota)
                .Where(s => s.ApplicationUserId == id)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            ViewBag.Solicitudes = solicitudes;

            return View(adoptante);
        }
    }
}