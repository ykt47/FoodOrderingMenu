using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodOrderingMenu.Services
{
    public interface ICaptchaService
    {
        Task<bool> VerifyCaptcha(string captchaResponse);
    }

    public class CaptchaService : ICaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public CaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _secretKey = _configuration["ReCaptcha:SecretKey"] ?? "";
        }

        public async Task<bool> VerifyCaptcha(string captchaResponse)
        {
            if (string.IsNullOrEmpty(captchaResponse))
            {
                return false;
            }

            try
            {
                var url = $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={captchaResponse}";

                var response = await _httpClient.PostAsync(url, null);
                var jsonString = await response.Content.ReadAsStringAsync();

                // Log the response for debugging
                Console.WriteLine($"Google reCAPTCHA Response: {jsonString}");

                // Use JsonSerializerOptions with PropertyNameCaseInsensitive
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var captchaResult = JsonSerializer.Deserialize<CaptchaVerificationResponse>(jsonString, options);

                if (captchaResult?.Success == true)
                {
                    Console.WriteLine("CAPTCHA verification SUCCESS");
                    return true;
                }
                else
                {
                    Console.WriteLine($"CAPTCHA verification FAILED. Error codes: {string.Join(", ", captchaResult?.ErrorCodes ?? new List<string>())}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Captcha verification error: {ex.Message}");
                return false;
            }
        }
    }

    public class CaptchaVerificationResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string>? ErrorCodes { get; set; }
    }
}