using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Todos los reportes son solo para Zoonosis: contienen datos de
    // adoptantes (teléfono, etc.) que no deben verse públicamente.
    [Authorize(Roles = "Zoonosis")]
    public class ReportesController(ApplicationDbContext context) : Controller
    {
        // Pantalla con las 3 tarjetas de reportes disponibles.
        public IActionResult Index() => View();

        // ---------- 1) Solicitudes por estado ----------
        // (la sección "Aprobadas" ya responde "quién adoptó qué y cuándo")
        public async Task<IActionResult> SolicitudesPorEstado()
        {
            var solicitudes = await context.Solicitudes
                .Include(s => s.Mascota)
                .ToListAsync();

            var pdf = ReportesPdfService.GenerarSolicitudesPorEstado(solicitudes);
            return File(pdf, "application/pdf", $"solicitudes-por-estado-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ---------- 2) Mascotas por sección, con su estado ----------
        // El modelo Mascota solo tiene "Disponible" (sí/no); "Pendiente" no es
        // un campo propio de la mascota, sino algo que se deduce: si tiene
        // una solicitud en estado Pendiente esperando revisión. Calcularlo así
        // evita agregar una columna nueva a la base de datos (por lo tanto,
        // no hace falta ninguna migración).
        public async Task<IActionResult> MascotasPorSeccion()
        {
            var mascotas = await context.Mascotas
                .OrderBy(m => m.Tipo)
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            var idsConSolicitudPendiente = (await context.Solicitudes
                .Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .Select(s => s.MascotaId)
                .ToListAsync())
                .ToHashSet();

            var filas = mascotas.Select(m =>
            {
                string estado = !m.Disponible
                    ? "Adoptada"
                    : idsConSolicitudPendiente.Contains(m.Id)
                        ? "Pendiente (en revisión)"
                        : "Disponible";

                return (Mascota: m, Estado: estado);
            }).ToList();

            var pdf = ReportesPdfService.GenerarMascotasPorSeccion(filas);
            return File(pdf, "application/pdf", $"mascotas-por-seccion-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ---------- 3) Adopciones por adoptante ----------
        public async Task<IActionResult> AdopcionesPorAdoptante()
        {
            var aprobadas = await context.Solicitudes
                .Include(s => s.Mascota)
                .Where(s => s.Estado == EstadoSolicitud.Aprobada)
                .OrderBy(s => s.Nombres).ThenBy(s => s.Apellidos)
                .ThenByDescending(s => s.FechaResolucion)
                .ToListAsync();

            var pdf = ReportesPdfService.GenerarAdopcionesPorAdoptante(aprobadas);
            return File(pdf, "application/pdf", $"adopciones-por-adoptante-{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}