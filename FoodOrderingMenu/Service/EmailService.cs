using System.Net;
using System.Net.Mail;

namespace FoodOrderingMenu.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _senderPassword;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            _senderName = _configuration["EmailSettings:SenderName"] ?? "Food Ordering System";
            _senderPassword = _configuration["EmailSettings:SenderPassword"] ?? "";
        }

        public async Task<bool> SendVerificationEmail(string toEmail, string fullName, string verificationLink)
        {
            var subject = "Verify Your Email Address";
            
            var htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 20px auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .header {{ background-color: #4CAF50; color: white; padding: 30px 20px; text-align: center; }}
                        .header h1 {{ margin: 0; font-size: 28px; }}
                        .content {{ padding: 30px; }}
                        .button {{ display: inline-block; padding: 15px 40px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ background-color: #f9f9f9; padding: 20px; text-align: center; color: #777; font-size: 12px; border-top: 1px solid #eee; }}
                        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🍽️ Welcome to Food Ordering System!</h1>
                        </div>
                        <div class='content'>
                            <p>Hi <strong>{fullName}</strong>,</p>
                            
                            <p>Thank you for registering! To complete your registration and start ordering delicious food, please verify your email address by clicking the button below:</p>
                            
                            <center>
                                <a href='{verificationLink}' class='button'>
                                    ✓ Verify Email Address
                                </a>
                            </center>
                            
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #2196F3;'>{verificationLink}</p>
                            
                            <div class='warning'>
                                <strong>⚠️ Important:</strong> This verification link will expire in 24 hours. If you didn't create an account, please ignore this email.
                            </div>
                            
                            <p>Once verified, you'll be able to:</p>
                            <ul>
                                <li>✓ Browse our menu</li>
                                <li>✓ Place orders</li>
                                <li>✓ Track your orders</li>
                                <li>✓ View order history</li>
                            </ul>
                            
                            <p>We're excited to serve you!</p>
                            
                            <p>Best regards,<br>Food Ordering Team</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Food Ordering System. All rights reserved.</p>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendPasswordResetEmail(string toEmail, string fullName, string resetLink)
        {
            var subject = "Password Reset Request";
            
            var htmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 20px auto; background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                        .header {{ background-color: #2196F3; color: white; padding: 30px 20px; text-align: center; }}
                        .header h1 {{ margin: 0; font-size: 28px; }}
                        .content {{ padding: 30px; }}
                        .button {{ display: inline-block; padding: 15px 40px; background-color: #2196F3; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ background-color: #f9f9f9; padding: 20px; text-align: center; color: #777; font-size: 12px; border-top: 1px solid #eee; }}
                        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔐 Password Reset Request</h1>
                        </div>
                        <div class='content'>
                            <p>Hi <strong>{fullName}</strong>,</p>
                            
                            <p>We received a request to reset your password. Click the button below to create a new password:</p>
                            
                            <center>
                                <a href='{resetLink}' class='button'>
                                    Reset Password
                                </a>
                            </center>
                            
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #2196F3;'>{resetLink}</p>
                            
                            <div class='warning'>
                                <strong>⚠️ Security Notice:</strong>
                                <ul style='margin: 10px 0;'>
                                    <li>This link will expire in 1 hour</li>
                                    <li>If you didn't request this, please ignore this email</li>
                                    <li>Your password will not change unless you click the link</li>
                                </ul>
                            </div>
                            
                            <p>For security reasons, we recommend choosing a strong password that includes:</p>
                            <ul>
                                <li>At least 8 characters</li>
                                <li>A mix of uppercase and lowercase letters</li>
                                <li>Numbers and special characters</li>
                            </ul>
                            
                            <p>If you have any questions, please contact our support team.</p>
                            
                            <p>Best regards,<br>Food Ordering Team</p>
                        </div>
                        <div class='footer'>
                            <p>© 2025 Food Ordering System. All rights reserved.</p>
                            <p>This is an automated email. Please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_senderEmail, _senderPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                
                Console.WriteLine($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
