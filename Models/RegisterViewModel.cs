using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcommerceApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [Display(Name = "Apellido")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "La edad es obligatoria")]
        [Range(18, 99, ErrorMessage = "Debes ser mayor de edad (18+) para solicitar una adopción")]
        [Display(Name = "Edad")]
        public int Edad { get; set; }

        [Required, EmailAddress, Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de teléfono es obligatorio")]
        [Phone(ErrorMessage = "Ingresa un número de teléfono válido")]
        [Display(Name = "Número de teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de carnet es obligatorio")]
        [Display(Name = "Número de carnet de identidad")]
        [StringLength(20)]
        public string NumeroCarnet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes subir la foto del anverso de tu carnet")]
        [Display(Name = "Foto carnet (anverso)")]
        public IFormFile FotoCarnetAnverso { get; set; } = null!;

        [Required(ErrorMessage = "Debes subir la foto del reverso de tu carnet")]
        [Display(Name = "Foto carnet (reverso)")]
        public IFormFile FotoCarnetReverso { get; set; } = null!;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}