using System.ComponentModel.DataAnnotations;

namespace FoodOrderingMenu.Models
{
    public class MenuCategory
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public int SortOrder { get; set; } = 0;

        public string Icon { get; set; } = "bi-circle";
    }
}
