using EcommerceApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcommerceApp.Services
{
    // ============================================================
    // GENERADOR DE REPORTES EN PDF (con la librería QuestPDF)
    // Tres reportes en total. Cada método arma UNO a partir de datos
    // que ya vienen consultados desde ReportesController (esta clase
    // no toca la base de datos, solo recibe listas y dibuja el PDF).
    //
    // Estilo: se toma como referencia la identidad visual del sitio
    // (el cartel "Adopta, no compres" del login: degradado morado→
    // rosado, formas redondeadas, huellitas como acento). No se usa
    // ninguna imagen — todo son formas simples dibujadas con código,
    // así el PDF no depende de descargar nada de internet.
    // ============================================================
    public static class ReportesPdfService
    {
        // Colores de la marca (los mismos que adoptapatitas.css).
        private const string ColorPrimario = "#5B3A8E";   // morado
        private const string ColorSecundario = "#D95B91"; // rosado
        private const string ColorCrema = "#FBEFE8";
        private const string ColorTextoSuave = "#6C6677";
        private const string ColorBordeSuave = "#F0DDE6";

        // ---------- 1) SOLICITUDES POR ESTADO ----------
        // (la sección "Aprobadas" ya responde "quién adoptó qué y cuándo")
        public static byte[] GenerarSolicitudesPorEstado(List<SolicitudAdopcion> solicitudes)
        {
            var pendientes = solicitudes.Where(s => s.Estado == EstadoSolicitud.Pendiente)
                .OrderByDescending(s => s.FechaSolicitud).ToList();
            var aprobadas = solicitudes.Where(s => s.Estado == EstadoSolicitud.Aprobada)
                .OrderByDescending(s => s.FechaResolucion).ToList();
            var rechazadas = solicitudes.Where(s => s.Estado == EstadoSolicitud.Rechazada)
                .OrderByDescending(s => s.FechaResolucion).ToList();

            return ConstruirPdf("Solicitudes de adopción por estado", contenido =>
            {
                contenido.Item().Text(
                    $"Total: {solicitudes.Count}   ·   Pendientes: {pendientes.Count}   ·   " +
                    $"Aprobadas: {aprobadas.Count}   ·   Rechazadas: {rechazadas.Count}")
                    .FontSize(9).FontColor(ColorTextoSuave);

                contenido.Item().PaddingTop(16).Element(e => TituloSeccion(e,
                    $"Pendientes ({pendientes.Count})", Colors.White, Colors.Orange.Medium));
                contenido.Item().PaddingTop(6).Element(e => TarjetaTabla(e, interior =>
                {
                    if (pendientes.Count == 0) { SinDatos(interior, "No hay solicitudes pendientes."); return; }
                    TablaSolicitudes(interior, pendientes, "Fecha solicitud", s => s.FechaSolicitud.ToString("dd/MM/yyyy"));
                }));

                contenido.Item().PaddingTop(20).Element(e => TituloSeccion(e,
                    $"Aprobadas ({aprobadas.Count})", Colors.White, Colors.Green.Medium));
                contenido.Item().PaddingTop(6).Element(e => TarjetaTabla(e, interior =>
                {
                    if (aprobadas.Count == 0) { SinDatos(interior, "No hay solicitudes aprobadas."); return; }
                    TablaSolicitudes(interior, aprobadas, "Fecha aprobación", s => s.FechaResolucion?.ToString("dd/MM/yyyy") ?? "-");
                }));

                contenido.Item().PaddingTop(20).Element(e => TituloSeccion(e,
                    $"Rechazadas ({rechazadas.Count})", Colors.White, Colors.Red.Medium));
                contenido.Item().PaddingTop(6).Element(e => TarjetaTabla(e, interior =>
                {
                    if (rechazadas.Count == 0) { SinDatos(interior, "No hay solicitudes rechazadas."); return; }
                    TablaSolicitudes(interior, rechazadas, "Motivo del rechazo", s => Truncar(s.Observacion, 55));
                }));
            });
        }

        // Tabla reutilizada por las 3 secciones de arriba: las primeras 3
        // columnas son siempre iguales (Adoptante, Mascota, Teléfono); la
        // última cambia según la sección (se recibe como parámetro).
        private static void TablaSolicitudes(IContainer contenedor, List<SolicitudAdopcion> filas,
            string columnaExtra, Func<SolicitudAdopcion, string> valorExtra)
        {
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(3); // Adoptante
                    columnas.RelativeColumn(3); // Mascota
                    columnas.RelativeColumn(2); // Teléfono
                    columnas.RelativeColumn(4); // columna extra
                });

                tabla.Header(encabezado =>
                {
                    CeldaEncabezado(encabezado.Cell(), "Adoptante");
                    CeldaEncabezado(encabezado.Cell(), "Mascota");
                    CeldaEncabezado(encabezado.Cell(), "Teléfono");
                    CeldaEncabezado(encabezado.Cell(), columnaExtra);
                });

                foreach (var s in filas)
                {
                    CeldaFila(tabla.Cell(), $"{s.Nombres} {s.Apellidos}");
                    CeldaFila(tabla.Cell(), s.Mascota?.Nombre ?? "-");
                    CeldaFila(tabla.Cell(), s.Telefono);
                    CeldaFila(tabla.Cell(), valorExtra(s));
                }
            });
        }

        // ---------- 2) MASCOTAS POR SECCIÓN, CON SU ESTADO ----------
        // "filas" ya viene con el estado calculado (Disponible / Pendiente /
        // Adoptada): esa lógica junta datos de Mascotas Y Solicitudes, así
        // que se resuelve antes, en ReportesController.
        public static byte[] GenerarMascotasPorSeccion(List<(Mascota Mascota, string Estado)> filas)
        {
            var perros = filas.Where(f => f.Mascota.Tipo == TipoMascota.Perro)
                .OrderBy(f => f.Mascota.Nombre).ToList();
            var gatos = filas.Where(f => f.Mascota.Tipo == TipoMascota.Gato)
                .OrderBy(f => f.Mascota.Nombre).ToList();

            var disponibles = filas.Count(f => f.Estado == "Disponible");
            var pendientes = filas.Count(f => f.Estado.StartsWith("Pendiente"));
            var adoptadas = filas.Count(f => f.Estado == "Adoptada");

            return ConstruirPdf("Mascotas por sección y su estado", contenido =>
            {
                contenido.Item().Text(
                    $"Total: {filas.Count}   ·   Disponibles: {disponibles}   ·   " +
                    $"Pendientes: {pendientes}   ·   Adoptadas: {adoptadas}")
                    .FontSize(9).FontColor(ColorTextoSuave);

                contenido.Item().PaddingTop(16).Element(e => TituloSeccion(e,
                    $"Perros ({perros.Count})", Colors.White, "#7A6BC7"));
                contenido.Item().PaddingTop(6).Element(e => TarjetaTabla(e, interior =>
                {
                    if (perros.Count == 0) { SinDatos(interior, "No hay perros registrados."); return; }
                    TablaMascotas(interior, perros);
                }));

                contenido.Item().PaddingTop(20).Element(e => TituloSeccion(e,
                    $"Gatos ({gatos.Count})", Colors.White, ColorSecundario));
                contenido.Item().PaddingTop(6).Element(e => TarjetaTabla(e, interior =>
                {
                    if (gatos.Count == 0) { SinDatos(interior, "No hay gatos registrados."); return; }
                    TablaMascotas(interior, gatos);
                }));
            });
        }

        private static void TablaMascotas(IContainer contenedor, List<(Mascota Mascota, string Estado)> filas)
        {
            contenedor.Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(2); // Nombre
                    columnas.RelativeColumn(2); // Fecha ingreso
                    columnas.RelativeColumn(2); // Color
                    columnas.RelativeColumn(5); // Descripción
                    columnas.RelativeColumn(2); // Estado
                });

                tabla.Header(encabezado =>
                {
                    CeldaEncabezado(encabezado.Cell(), "Nombre");
                    CeldaEncabezado(encabezado.Cell(), "Fecha ingreso");
                    CeldaEncabezado(encabezado.Cell(), "Color");
                    CeldaEncabezado(encabezado.Cell(), "Descripción");
                    CeldaEncabezado(encabezado.Cell(), "Estado");
                });

                foreach (var (m, estado) in filas)
                {
                    CeldaFila(tabla.Cell(), m.Nombre);
                    CeldaFila(tabla.Cell(), m.FechaIngreso.ToString("dd/MM/yyyy"));
                    CeldaFila(tabla.Cell(), string.IsNullOrEmpty(m.Color) ? "-" : m.Color);
                    CeldaFila(tabla.Cell(), Truncar(m.Descripcion, 65));
                    CeldaFila(tabla.Cell(), estado);
                }
            });
        }

        // ---------- 3) ADOPCIONES POR ADOPTANTE ----------
        public static byte[] GenerarAdopcionesPorAdoptante(List<SolicitudAdopcion> aprobadas)
        {
            return ConstruirPdf("Adopciones por adoptante", contenido =>
            {
                contenido.Item().Text($"Total de adopciones concretadas: {aprobadas.Count}")
                    .FontSize(9).FontColor(ColorTextoSuave);

                contenido.Item().PaddingTop(14).Element(e => TarjetaTabla(e, interior =>
                {
                    if (aprobadas.Count == 0) { SinDatos(interior, "Todavía no hay ninguna adopción aprobada."); return; }

                    interior.Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn(3); // Adoptante
                            columnas.RelativeColumn(2); // Teléfono
                            columnas.RelativeColumn(3); // Mascota
                            columnas.RelativeColumn(2); // Fecha de adopción
                        });

                        tabla.Header(encabezado =>
                        {
                            CeldaEncabezado(encabezado.Cell(), "Adoptante");
                            CeldaEncabezado(encabezado.Cell(), "Teléfono");
                            CeldaEncabezado(encabezado.Cell(), "Mascota adoptada");
                            CeldaEncabezado(encabezado.Cell(), "Fecha de adopción");
                        });

                        foreach (var s in aprobadas)
                        {
                            CeldaFila(tabla.Cell(), $"{s.Nombres} {s.Apellidos}");
                            CeldaFila(tabla.Cell(), s.Telefono);
                            CeldaFila(tabla.Cell(), s.Mascota?.Nombre ?? "-");
                            CeldaFila(tabla.Cell(), s.FechaResolucion?.ToString("dd/MM/yyyy") ?? "-");
                        }
                    });
                }));
            });
        }

        // ============================================================
        // ---------- Piezas reutilizables (logo, encabezado, tarjetas) ----------
        // ============================================================

        // Arma el documento completo: encabezado con degradado + logo de
        // texto, el contenido que llena cada método de arriba, y el pie
        // de página con el número de página.
        private static byte[] ConstruirPdf(string titulo, Action<ColumnDescriptor> construirContenido)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(26);
                    // "DejaVu Sans" es la fuente que se instala en el Dockerfile:
                    // sin ese paso, en Render (Linux) no habría NINGUNA fuente
                    // disponible y la generación del PDF fallaría.
                    pagina.DefaultTextStyle(x => x.FontSize(9).FontFamily("DejaVu Sans"));

                    pagina.Header().Element(e => Encabezado(e, titulo));
                    pagina.Content().PaddingTop(14).Column(construirContenido);

                    pagina.Footer().AlignCenter().Text(pie =>
                    {
                        pie.Span("AdoptaPatitas  ·  Página ").FontSize(8).FontColor(ColorTextoSuave);
                        pie.CurrentPageNumber().FontSize(8).FontColor(ColorTextoSuave);
                        pie.Span(" de ").FontSize(8).FontColor(ColorTextoSuave);
                        pie.TotalPages().FontSize(8).FontColor(ColorTextoSuave);
                    });
                });
            }).GeneratePdf();
        }

        // Encabezado de cada reporte: franja con degradado morado→rosado y
        // esquinas redondeadas (mismo estilo que las tarjetas y botones del
        // sitio), con el logo de texto "AdoptaPatitas", el nombre del
        // reporte, la fecha de generación, y una fila de puntitos de color
        // como acento (en lugar de la ilustración de huellitas, para no
        // depender de ninguna imagen).
        private static void Encabezado(IContainer contenedor, string titulo)
        {
            contenedor
                .BackgroundLinearGradient(120, new QuestPDF.Infrastructure.Color[] { ColorPrimario, ColorSecundario })
                .CornerRadius(16)
                .Padding(18)
                .Row(fila =>
                {
                    fila.RelativeItem().Column(columna =>
                    {
                        columna.Item().Text(texto =>
                        {
                            texto.Span("Adopta").FontSize(21).Bold().FontColor(Colors.White);
                            texto.Span("Patitas").FontSize(21).Bold().FontColor("#FFE3EF");
                        });

                        // Puntitos decorativos (acento tipo "huellitas", sin
                        // depender de un emoji ni de una imagen).
                        columna.Item().PaddingTop(4).Row(puntos =>
                        {
                            puntos.Spacing(5);
                            foreach (var _ in Enumerable.Range(0, 4))
                                puntos.ConstantItem(6).Height(6).Background("#FFFFFF").CornerRadius(3);
                            puntos.RelativeItem(); // empuja los puntos a la izquierda
                        });

                        columna.Item().PaddingTop(8).Text(titulo).FontSize(13).SemiBold().FontColor(Colors.White);
                    });

                    fila.ConstantItem(120).AlignRight().AlignBottom().Text(
                        $"Generado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}")
                        .FontSize(7.5f).FontColor("#FFE3EF");
                });
        }

        // "Chip" de color para el título de cada sección (Pendientes,
        // Aprobadas, Perros, Gatos...), con forma de pastilla — el mismo
        // estilo que los botones redondeados del sitio.
        private static void TituloSeccion(IContainer contenedor, string texto, string colorTexto, string colorFondo)
        {
            contenedor.Row(fila =>
            {
                fila.AutoItem()
                    .Background(colorFondo)
                    .CornerRadius(20)
                    .PaddingVertical(5).PaddingHorizontal(16)
                    .Text(texto).FontSize(10.5f).Bold().FontColor(colorTexto);

                fila.RelativeItem(); // deja el resto de la fila vacío
            });
        }

        // Envuelve una tabla en una "tarjeta" blanca con esquinas
        // redondeadas y un borde suave — el mismo lenguaje visual de las
        // tarjetas del catálogo y del panel de administración del sitio.
        private static void TarjetaTabla(IContainer contenedor, Action<IContainer> construirContenido)
        {
            contenedor
                .Background(Colors.White)
                .Border(1).BorderColor(ColorBordeSuave)
                .CornerRadius(14)
                .Padding(4)
                // El truco: como Table no admite CornerRadius directo, se
                // redondea el CONTENEDOR que lo envuelve (arriba) y se deja
                // la tabla cuadrada por dentro — así no se ve "cortada" en
                // las esquinas. IMPORTANTE: el contenido se dibuja sobre
                // "interior" (el contenedor que entrega Element), nunca
                // sobre "contenedor" de nuevo — usar el mismo contenedor dos
                // veces es justo lo que QuestPDF prohíbe ("multiple child
                // elements to a single-child container").
                .Element(interior => construirContenido(interior));
        }

        private static void SinDatos(IContainer contenedor, string mensaje)
        {
            contenedor.Padding(14).Text(mensaje).Italic().FontColor(ColorTextoSuave);
        }

        // Celda de encabezado de tabla: fondo crema y texto en negrita.
        private static void CeldaEncabezado(IContainer celda, string texto)
        {
            celda.Background(ColorCrema).Padding(6)
                .Text(texto).FontSize(8.5f).Bold().FontColor(ColorPrimario);
        }

        // Celda normal de una fila de datos.
        private static void CeldaFila(IContainer celda, string texto)
        {
            celda.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(6)
                .Text(string.IsNullOrEmpty(texto) ? "-" : texto).FontSize(8.5f);
        }

        // Recorta un texto largo (la descripción de una mascota, el motivo
        // de un rechazo) para que no desarme la tabla. Si no hay texto,
        // muestra "-".
        private static string Truncar(string? texto, int maximo)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "-";
            texto = texto.Trim();
            return texto.Length <= maximo ? texto : texto[..maximo] + "…";
        }
    }
}