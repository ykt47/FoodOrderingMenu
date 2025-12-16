// Register - With CAPTCHA reset on error
window.addEventListener('load', function () {

    // Reset CAPTCHA if there's an error message on page load
    var errorAlert = document.querySelector('.alert-danger');
    if (errorAlert && typeof grecaptcha !== 'undefined') {
        grecaptcha.reset();
    }

    // Password toggle functionality
    var togglePassword = document.getElementById("togglePassword");
    if (togglePassword) {
        togglePassword.addEventListener("click", function () {
            var passwordInput = document.getElementById("passwordInput");
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

    // Confirm password toggle
    var toggleConfirmPassword = document.getElementById("toggleConfirmPassword");
    if (toggleConfirmPassword) {
        toggleConfirmPassword.addEventListener("click", function () {
            var confirmPasswordInput = document.getElementById("confirmPasswordInput");
            var eyeIcon2 = document.getElementById("eyeIcon2");
            if (confirmPasswordInput.type === "password") {
                confirmPasswordInput.type = "text";
                eyeIcon2.innerText = "Hide";
            } else {
                confirmPasswordInput.type = "password";
                eyeIcon2.innerText = "Show";
            }
        });
    }

    // Real-time password match validation
    var confirmPasswordInput = document.getElementById("confirmPasswordInput");
    var passwordInput = document.getElementById("passwordInput");
    if (confirmPasswordInput && passwordInput) {
        confirmPasswordInput.addEventListener("input", function () {
            var password = passwordInput.value;
            var confirmPassword = confirmPasswordInput.value;
            var errorSpan = document.getElementById("confirmPasswordError");
            if (password !== confirmPassword && confirmPassword.length > 0) {
                errorSpan.classList.remove("d-none");
            } else {
                errorSpan.classList.add("d-none");
            }
        });
    }

    // Form validation
    var form = document.getElementById('registerForm');

    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            var fullName = document.getElementById("fullNameInput").value.trim();
            var email = document.getElementById("emailInput").value.trim();
            var password = passwordInput.value;
            var confirmPassword = confirmPasswordInput.value;
            var isValid = true;

            // Reset errors
            document.getElementById("fullNameError").classList.add("d-none");
            document.getElementById("emailError").classList.add("d-none");
            document.getElementById("passwordError").classList.add("d-none");
            document.getElementById("confirmPasswordError").classList.add("d-none");
            document.getElementById("captchaError").textContent = "";

            // Validate fields
            if (!fullName) {
                document.getElementById("fullNameError").classList.remove("d-none");
                isValid = false;
            }
            if (!email || email.indexOf("@") === -1) {
                document.getElementById("emailError").classList.remove("d-none");
                isValid = false;
            }
            if (!password || password.length < 6) {
                document.getElementById("passwordError").classList.remove("d-none");
                isValid = false;
            }
            if (password !== confirmPassword) {
                document.getElementById("confirmPasswordError").classList.remove("d-none");
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
            // The CAPTCHA will be automatically included in the form data
            form.submit();
        });
    }
});