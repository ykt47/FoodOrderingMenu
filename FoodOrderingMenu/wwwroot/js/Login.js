// Login - With CAPTCHA reset on error
window.addEventListener('load', function () {

    // Reset CAPTCHA if there's an error message on page load
    var errorAlert = document.querySelector('.alert-danger');
    if (errorAlert && typeof grecaptcha !== 'undefined') {
        grecaptcha.reset();
    }

    // Show/Hide password
    var togglePassword = document.getElementById("togglePassword");
    var passwordInput = document.getElementById("passwordInput");
    if (togglePassword && passwordInput) {
        togglePassword.addEventListener("click", function () {
            var eyeIcon = document.getElementById("eyeIcon");
            if (passwordInput.type === "password") {
                passwordInput.type = "text";
                eyeIcon.innerText = "Hide";
            } else {
                passwordInput.type = "password";
                eyeIcon.innerText = "Show";
            }
        });
    }

    // Form validation
    var form = document.getElementById('loginForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            var email = document.getElementById("emailInput").value.trim();
            var pw = passwordInput.value.trim();
            var isValid = true;

            // Reset errors
            document.getElementById("emailError").classList.add("d-none");
            document.getElementById("passwordError").classList.add("d-none");
            document.getElementById("captchaError").textContent = "";

            // Validate fields
            if (!email) {
                document.getElementById("emailError").classList.remove("d-none");
                isValid = false;
            }
            if (!pw) {
                document.getElementById("passwordError").classList.remove("d-none");
                isValid = false;
            }

            // Get CAPTCHA response
            var captchaResponse = grecaptcha.getResponse();

            if (!captchaResponse || captchaResponse.length === 0) {
                document.getElementById("captchaError").textContent = "Please complete the CAPTCHA verification";
                isValid = false;
            }

            if (!isValid) {
                return false;
            }

            // All validation passed - submit the form
            form.submit();
        });
    }
});