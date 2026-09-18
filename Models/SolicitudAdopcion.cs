using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace EcommerceApp.Models
{
    // Los tres estados posibles de una solicitud. Se guarda como texto
    // en la base de datos (ver ApplicationDbContext) para que sea legible
    // si alguien mira la tabla directo en Supabase.
    public enum EstadoSolicitud
    {
        Pendiente,
        Aprobada,
        Rechazada
    }

    public class SolicitudAdopcion
    {
        [Key]
        public int Id { get; set; }

        // ---------- A qué mascota y quién la pide ----------
        [Required]
        public int MascotaId { get; set; }
        public Mascota Mascota { get; set; } = null!;

        // El Id del ApplicationUser que hizo la solicitud (para poder
        // filtrar "mis solicitudes" y evitar duplicados).
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        // ---------- Datos formales del interesado ----------
        // A diferencia del registro (que es simple), aquí SÍ se piden
        // todos los datos que Zoonosis necesita para evaluar la adopción.

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "El nombre solo puede contener letras")]
        [MaxLength(50)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "El apellido solo puede contener letras")]
        [MaxLength(50)]
        public string Apellidos { get; set; } = string.Empty;

        // Rango 18-60 según lo que definiste: ni menores de edad, ni
        // adultos mayores (para quienes el compromiso a largo plazo con
        // una mascota puede ser más difícil de sostener).
        [Required(ErrorMessage = "La edad es obligatoria")]
        [Range(18, 60, ErrorMessage = "La edad debe estar entre 18 y 60 años")]
        public int Edad { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El teléfono solo puede contener números")]
        [StringLength(15, MinimumLength = 7)]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indica tu domicilio")]
        [Display(Name = "Domicilio")]
        [MaxLength(200)]
        public string DondeVive { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de carnet es obligatorio")]
        [Display(Name = "Número de carnet")]
        [MaxLength(20)]
        public string NumeroCarnet { get; set; } = string.Empty;

        // Las fotos de carnet AHORA son obligatorias aquí — como el
        // registro ya no las pide, esta es la única oportunidad de
        // verificar identidad antes de aprobar una adopción.
        [Required(ErrorMessage = "Debes subir la foto del anverso de tu carnet")]
        [Display(Name = "Foto carnet (anverso)")]
        [NotMapped]
        public IFormFile? FotoCarnetAnversoArchivo { get; set; }

        [Required(ErrorMessage = "Debes subir la foto del reverso de tu carnet")]
        [Display(Name = "Foto carnet (reverso)")]
        [NotMapped]
        public IFormFile? FotoCarnetReversoArchivo { get; set; }

        // Rutas ya subidas a Supabase Storage (las llena el controlador,
        // no las escribe la persona directamente).
        public string? FotoCarnetAnversoUrl { get; set; }
        public string? FotoCarnetReversoUrl { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Comparte una red social de contacto")]
        [Display(Name = "Red social (Facebook, Instagram, etc.)")]
        [MaxLength(150)]
        public string RedSocial { get; set; } = string.Empty;

        // Nueva pregunta que pediste: si ya tiene mascotas, importa saber
        // en qué condiciones (para evaluar si el hogar es apto).
        [Display(Name = "¿Tienes otras mascotas actualmente?")]
        public bool TieneOtrasMascotas { get; set; }

        [MaxLength(300)]
        [Display(Name = "Cuéntanos sobre tus otras mascotas (opcional)")]
        public string? DetalleOtrasMascotas { get; set; }

        // Sigue siendo opcional, tal como pediste desde el principio.
        [MaxLength(500)]
        [Display(Name = "¿Por qué quieres adoptar? (opcional)")]
        public string? Motivo { get; set; }

        // ---------- Estado del proceso ----------
        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        // Obligatorio al rechazar, para que la persona sepa qué corregir.
        [MaxLength(500)]
        public string? Observacion { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public DateTime? FechaResolucion { get; set; }
    }
}