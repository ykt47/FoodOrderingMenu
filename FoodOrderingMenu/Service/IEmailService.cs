namespace FoodOrderingMenu.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Send email verification link
        /// </summary>
        Task<bool> SendVerificationEmail(string toEmail, string fullName, string verificationLink);
        
        /// <summary>
        /// Send password reset email
        /// </summary>
        Task<bool> SendPasswordResetEmail(string toEmail, string fullName, string resetLink);
    }
}
