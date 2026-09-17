using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcommerceApp.Models
{
    public class CompletarPerfilViewModel
    {
        [Required(ErrorMessage = "El apellido es obligatorio")]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "La edad es obligatoria")]
        [Range(18, 99, ErrorMessage = "Debes ser mayor de edad (18+) para solicitar una adopción")]
        [Display(Name = "Edad")]
        public int Edad { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio")]
        [Phone(ErrorMessage = "Ingresa un número de teléfono válido")]
        [Display(Name = "Número de teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de carnet es obligatorio")]
        [Display(Name = "Número de carnet de identidad")]
        public string NumeroCarnet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes subir la foto del anverso de tu carnet")]
        [Display(Name = "Foto carnet (anverso)")]
        public IFormFile FotoCarnetAnverso { get; set; } = null!;

        [Required(ErrorMessage = "Debes subir la foto del reverso de tu carnet")]
        [Display(Name = "Foto carnet (reverso)")]
        public IFormFile FotoCarnetReverso { get; set; } = null!;
    }
}