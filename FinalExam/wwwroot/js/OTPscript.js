document.addEventListener("DOMContentLoaded", () => {
    const otpInputs = document.querySelectorAll(".otp-card-inputs input");

    otpInputs.forEach((input, index) => {
        input.addEventListener("input", (e) => {
            const value = e.target.value;

            // Ensure only numbers are allowed
            if (!/^\d$/.test(value)) {
                e.target.value = "";
                return;
            }

            // Move to the next input field if there's a valid digit
            if (value && index < otpInputs.length - 1) {
                otpInputs[index + 1].focus();
            }
        });

        input.addEventListener("keydown", (e) => {
            if (e.key === "Backspace") {
                // Clear current input and move focus to the previous field if empty
                if (!input.value && index > 0) {
                    otpInputs[index - 1].focus();
                }
            } else if (!/^\d$/.test(e.key) && e.key !== "Backspace" && e.key !== "Tab") {
                // Prevent any non-numeric or non-functional keys
                e.preventDefault();
            }
        });

        input.addEventListener("paste", (e) => {
            e.preventDefault();

            // Handle pasting of OTP (e.g., user pastes the entire code)
            const data = e.clipboardData.getData("text").replace(/\D/g, "");
            if (data.length === otpInputs.length) {
                otpInputs.forEach((field, idx) => {
                    field.value = data[idx] || "";
                });
                otpInputs[otpInputs.length - 1].focus(); // Move to the last field
            }
        });

        input.addEventListener("focus", () => {
            // Clear input when focused to ensure single focus input behavior
            input.select();
        });
    });

    // Button validation
    const verifyButton = document.querySelector(".otp-card button");
    const validateInputs = () => {
        verifyButton.disabled = !Array.from(otpInputs).every((input) => input.value.trim() !== "");
    };

    otpInputs.forEach((input) => {
        input.addEventListener("input", validateInputs);
    });
});
