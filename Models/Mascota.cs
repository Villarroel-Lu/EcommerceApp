using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace EcommerceApp.Models
{
    public enum TipoMascota
    {
        Perro,
        Gato
    }

    public enum SexoMascota
    {
        Macho,
        Hembra
    }

    public class Mascota
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), MaxLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria"), MaxLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo")]
        public TipoMascota Tipo { get; set; }

        [MaxLength(50)]
        public string? Raza { get; set; }

        [MaxLength(30)]
        public string? Color { get; set; }

        [Required(ErrorMessage = "La edad es obligatoria"), MaxLength(20)]
        public string Edad { get; set; } = string.Empty;

        [Required]
        public SexoMascota Sexo { get; set; }

        [Required(ErrorMessage = "La ubicación es obligatoria"), MaxLength(100)]
        public string Ubicacion { get; set; } = "Cochabamba";

        // ---------- SALUD: visible para TODOS (adoptante y Zoonosis) ----------

        // Lista de estados de salud seleccionados con checkboxes en el
        // formulario (Vacunado, Desparasitado, Esterilizado, Castrado...).
        // Npgsql mapea List<string> directo a un array de PostgreSQL
        // (columna tipo text[]), no hace falta ninguna conversión manual.
        [Display(Name = "Estados de salud")]
        public List<string> EstadosSalud { get; set; } = new();

        // Opciones fijas que se muestran como checkboxes en Create/Edit.
        // Están aquí (no repetidas en cada vista) para que sea un solo
        // lugar donde agregar una opción nueva en el futuro. Al ser
        // "static", Entity Framework la ignora automáticamente (no
        // intenta crear una columna para esto).
        public static readonly string[] OpcionesEstadoSalud =
        {
            "Saludable", "Vacunado", "Desparasitado", "Esterilizado", "Castrado"
        };

        // Banderas simples que el adoptante SÍ puede ver, sin necesidad
        // de conocer el detalle clínico exacto.
        [Display(Name = "¿Está en tratamiento actualmente?")]
        public bool EnTratamiento { get; set; }

        [Display(Name = "¿Tiene alguna enfermedad de base?")]
        public bool TieneEnfermedadBase { get; set; }

        [Display(Name = "¿Necesita cuidados a largo plazo / de por vida?")]
        public bool RequiereCuidadosLargoPlazo { get; set; }

        // Texto breve y general para el adoptante (ej: "Necesita medicación
        // diaria de por vida"), SIN detalles clínicos internos.
        [MaxLength(300)]
        [Display(Name = "Resumen de cuidados (visible para el adoptante)")]
        public string? ResumenCuidadosAdoptante { get; set; }

        // ---------- SALUD: SOLO visible para Zoonosis ----------
        // Estos dos campos nunca se muestran en las vistas públicas
        // (Index, Details para un Adoptante) — solo cuando quien mira
        // la página tiene el rol "Zoonosis". Ver Details.cshtml.

        [MaxLength(500)]
        [Display(Name = "Condición en la que llegó (uso interno)")]
        public string? CondicionLlegada { get; set; }

        [MaxLength(500)]
        [Display(Name = "Tratamiento a aplicar (uso interno)")]
        public string? TratamientoAdministrativo { get; set; }

        // ---------- Resto de campos ----------

        public bool Disponible { get; set; } = true;

        public string? FotoUrl { get; set; }

        [NotMapped]
        [Display(Name = "Foto")]
        public IFormFile? FotoArchivo { get; set; }

        [Display(Name = "Fecha de ingreso")]
        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }
    }
}