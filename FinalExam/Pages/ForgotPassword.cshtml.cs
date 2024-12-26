using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;

namespace FinalExam.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? connString = null;

        public ForgotPasswordModel(IConfiguration configuration)
        {
            this._configuration = configuration;
            connString = _configuration.GetConnectionString("MyConn");
        }

        // -- Variable to hold output messages
        public string Message { get; set; } = "";

        // -- Variable to hold the color of the messages
        public string MessageColor { get; set; } = "black";

        public void OnGet()
        {
        }

        public void OnPost()
        {
            try
            {
                string email = Request.Form["Email"];

                if (string.IsNullOrEmpty(email))
                {
                    Message = "Please provide a valid email.";
                    MessageColor = "danger";
                    return;
                }

                // Check if the email exists
                if (CheckIfEmailExists(email))
                {
                    // Generate a token
                    string token = Guid.NewGuid().ToString();
                    string resetLink = Url.Page("/ResetPassword", null, new { email = email, token = token }, Request.Scheme);

                    // Save the token to the database
                    SaveResetToken(email, token);

                    // Send the reset email
                    if (SendResetEmail(email, resetLink))
                    {
                        Message = "A password reset link has been sent to your email.";
                        MessageColor = "success";
                    }
                    else
                    {
                        Message = "Failed to send the reset email. Please try again.";
                        MessageColor = "danger";
                    }
                }
                else
                {
                    Message = "This email is not registered.";
                    MessageColor = "danger";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Message = "An error occurred while processing your request. Please try again.";
                MessageColor = "danger";
            }
        }

        private bool CheckIfEmailExists(string email)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "SELECT COUNT(*) FROM Users WHERE Email = @_1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@_1", email);
                        conn.Open();
                        int count = (int)cmd.ExecuteScalar();
                        conn.Close();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking email existence: {ex.Message}");
                return false;
            }
        }

        private void SaveResetToken(string email, string token)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "UPDATE Users SET PasswordResetToken = @_2, TokenExpirationTime = @_3 WHERE Email = @_1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@_1", email);
                        cmd.Parameters.AddWithValue("@_2", token);
                        cmd.Parameters.AddWithValue("@_3", DateTime.Now.AddHours(1)); // Token valid for 1 hour
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving reset token: {ex.Message}");
            }
        }

        private bool SendResetEmail(string to, string resetLink)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("linuxkevin2024@gmail.com", "ysqqnkufjckksxjf"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("linuxkevin2024@gmail.com"),
                    Subject = "Password Reset Request",
                    Body = $"Click the link below to reset your password:\n\n{resetLink}",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(to);

                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reset email: {ex.Message}");
                return false;
            }
        }
    }
}
