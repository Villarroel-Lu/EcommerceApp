using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Registro simple a propósito: los datos "formales" (carnet, edad,
    // domicilio, motivo, etc.) ya no se piden aquí. Se piden más adelante,
    // en el momento real en que importan: al solicitar una adopción
    // (ver SolicitudAdopcion.cs).
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "El nombre solo puede contener letras")]
        [Display(Name = "Nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "El apellido solo puede contener letras")]
        [Display(Name = "Apellido")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required, EmailAddress, Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}