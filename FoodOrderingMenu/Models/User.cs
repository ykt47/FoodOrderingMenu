using System.ComponentModel.DataAnnotations;

namespace FoodOrderingMenu.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required] public string FullName { get; set; } = "";
        [Required][EmailAddress] public string Email { get; set; } = "";
        [Required] public string PasswordHash { get; set; } = "";
        public bool EmailConfirmed { get; set; } = false;
        public string Role { get; set; } = "Customer"; // Admin, Customer, Guest (guest not stored normally)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
