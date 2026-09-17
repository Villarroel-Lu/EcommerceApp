using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Dog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Age { get; set; }

        [MaxLength(100)]
        public string Breed { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Size { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsAdopted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
