using Microsoft.AspNetCore.Identity;

namespace EcommerceApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public int Edad { get; set; }
        public string? NumeroCarnet { get; set; }

        // Rutas relativas (dentro de wwwroot) donde quedan guardadas las fotos del carnet
        public string? FotoCarnetAnversoUrl { get; set; }
        public string? FotoCarnetReversoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}