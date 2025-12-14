using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace FoodOrderingMenu.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = "";

        public bool IsAvailable { get; set; } = true;

        public int? CategoryId { get; set; }
        public MenuCategory? Category { get; set; }

        // Not stored in DB — only for upload
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
