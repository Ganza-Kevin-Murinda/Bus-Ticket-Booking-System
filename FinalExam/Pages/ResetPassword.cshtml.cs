using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? connString = null;

        public ResetPasswordModel(IConfiguration configuration)
        {
            this._configuration = configuration;
            connString = _configuration.GetConnectionString("MyConn");
        }

        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Token { get; set; }
        [BindProperty]
        public string NewPassword { get; set; }

        public string Message { get; set; }
        public string MessageColor { get; set; }

        public IActionResult OnPost()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "SELECT TokenExpirationTime FROM Users WHERE Email = @_1 AND PasswordResetToken = @_2";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@_1", Email);
                        cmd.Parameters.AddWithValue("@_2", Token);
                        conn.Open();
                        DateTime? tokenExpirationTime = (DateTime?)cmd.ExecuteScalar();
                        conn.Close();

                        if (tokenExpirationTime == null || tokenExpirationTime < DateTime.Now)
                        {
                            Message = "Invalid or expired token.";
                            MessageColor = "danger";
                            return Page();
                        }

                        // Update password
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                        query = "UPDATE Users SET password = @_3, PasswordResetToken = NULL, TokenExpirationTime = NULL WHERE Email = @_1";
                        using (SqlCommand updateCmd = new SqlCommand(query, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@_1", Email);
                            updateCmd.Parameters.AddWithValue("@_3", hashedPassword);
                            conn.Open();
                            updateCmd.ExecuteNonQuery();
                            conn.Close();

                            Message = "Password has been reset successfully!";
                            MessageColor = "success";
                            return RedirectToPage("/Signin");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting password: {ex.Message}");
                Message = "An error occurred while resetting your password. Please try again.";
                MessageColor = "danger";
                return Page();
            }
        }
    }
}
