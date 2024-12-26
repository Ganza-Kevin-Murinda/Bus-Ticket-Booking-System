using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class OTPModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string? connString = null;

        public OTPModel(IConfiguration configuration)
        {
            this._configuration = configuration;
            connString = _configuration.GetConnectionString("MyConn");
        }

        // --variable to hold Output Messages
        public string Message { get; set; } = "";

        // --variable to hold the Color of the Messages
        public string MessageColor { get; set; } = "black";

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }


        public void OnGet()
        {
        }
        public void OnPost()
        {
            try
            {
                string userOtp = Request.Form["otp1"] +
                                 Request.Form["otp2"] +
                                 Request.Form["otp3"] +
                                 Request.Form["otp4"] +
                                 Request.Form["otp5"] +
                                 Request.Form["otp6"];

                Console.WriteLine($"User OTP: {userOtp}");
                Console.WriteLine($"Email: {Email}");

                if (!string.IsNullOrEmpty(Email))
                {
                    string message;
                    if (VerifyOtp(Email, userOtp, out message))
                    {
                        Console.WriteLine("OTP verified successfully.");

                        // Retrieve the redirect parameter
                        string redirectUrl = Request.Query["redirect"];
                        if (!string.IsNullOrEmpty(redirectUrl))
                        {
                            Response.Redirect(redirectUrl);
                        }
                        else
                        {
                            // Default behavior if no redirect parameter is provided
                            Response.Redirect("/Signin");
                        }
                    }
                    else
                    {
                        Console.WriteLine("OTP verification failed.");
                        Message = message;
                        MessageColor = "danger";
                    }
                }
                else
                {
                    Console.WriteLine("Email is null or empty.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }



        private bool VerifyOtp(string email, string userOtp, out string message)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    Console.WriteLine($"Connecting to database to verify OTP for email: {email}");

                    string query = "SELECT OTP FROM Users WHERE Email = @_1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@_1", email);
                        conn.Open();

                        string? storedOtp = cmd.ExecuteScalar()?.ToString();
                        conn.Close();

                        Console.WriteLine($"Stored OTP: {storedOtp}, User OTP: {userOtp}");

                        if (storedOtp == userOtp)
                        {
                            // Update IsVerified flag
                            query = "UPDATE Users SET isActive = 1 WHERE Email = @_1";
                            using (SqlCommand updateCmd = new SqlCommand(query, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@_1", email);
                                conn.Open();
                                updateCmd.ExecuteNonQuery();
                                conn.Close();

                                Console.WriteLine("isActive column updated successfully.");
                            }

                            message = "OTP Verified Successfully!";
                            return true;
                        }
                        else
                        {
                            message = "Invalid OTP. Please try again.";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in VerifyOtp: {ex.Message}");
                message = $"Error during OTP verification: {ex.Message}";
                return false;
            }
        }
    }
}
