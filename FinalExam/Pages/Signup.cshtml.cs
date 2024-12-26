using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Mail;
using BCrypt.Net;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace FinalExam.Pages
{
    public class SignupModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string? connString = null;

        public SignupModel(IConfiguration configuration)
        {
            this._configuration = configuration;
            connString = _configuration.GetConnectionString("MyConn");
        }

        // Instantiating the Users class
        public Users theUsers = new Users();

        // --variable to hold Output Messages
        public string Message { get; set; } = "";

        // --variable to hold the Color of the Messages
        public string MessageColor { get; set; } = "black";



        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // -- validations
            if (string.IsNullOrEmpty(Request.Form["Name"]) ||
                string.IsNullOrEmpty(Request.Form["Phone"]) ||
                string.IsNullOrEmpty(Request.Form["Email"]) ||
                string.IsNullOrEmpty(Request.Form["PasswordHash"]))
            {
                Message = "All Fields are required";
                MessageColor = "danger";

                // --return page
                return Page();
            }

            // Get values from the form
            string? name = Request.Form["Name"];
            string? phone = Request.Form["Phone"];
            theUsers.Email = Request.Form["Email"];
            string? plainPassword = Request.Form["PasswordHash"];

            // Hash the password
            theUsers.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            // Generate OTP
            string otp = new Random().Next(100000, 999999).ToString(); // Random 6-digit OTP

            // -- save the data
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // First Insert into Users table
                    string insertUserQuery = @"
                INSERT INTO Users (email, password, OTP, isActive)
                OUTPUT INSERTED.userId
                VALUES (@Email, @PasswordHash, @OTP, @IsActive)";
                    using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", theUsers.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", theUsers.PasswordHash);
                        cmd.Parameters.AddWithValue("@OTP", otp);
                        cmd.Parameters.AddWithValue("@IsActive", false);

                        // Retrieve UserId from the OUTPUT clause
                        int userId = (int)cmd.ExecuteScalar();

                        // Insert into Client table
                        string insertClientQuery = @"
                    INSERT INTO Clients (name, phoneNo, userId)
                    VALUES (@Name, @Phone, @UserId)";
                        using (SqlCommand cmd1 = new SqlCommand(insertClientQuery, conn))
                        {
                            cmd1.Parameters.AddWithValue("@Name", name);
                            cmd1.Parameters.AddWithValue("@Phone", phone);
                            cmd1.Parameters.AddWithValue("@UserId", userId);

                            cmd1.ExecuteNonQuery();
                        }

                        // Send OTP via email
                        SendOtpEmail(theUsers.Email, otp);

                        // Log redirection
                        Console.WriteLine($"Redirecting to OTP page for Email: {theUsers.Email}");
                        return Redirect($"/OTP?email={theUsers.Email}&redirect=/Signin");
                    }
                }
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                MessageColor = "danger";
            }

            // Default return in case of errors
            return Page();
        }


        public void SendOtpEmail(string email, string otp)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("linuxkevin2024@gmail.com");
                mail.To.Add(email);
                mail.Subject = "Your OTP for Signup";
                mail.Body = $"Your OTP is: {otp}";

                smtpServer.Port = 587; // Example for SMTP
                smtpServer.Credentials = new System.Net.NetworkCredential("linuxkevin2024@gmail.com", "ysqqnkufjckksxjf");
                smtpServer.EnableSsl = true;

                smtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                // Log or handle email sending errors
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
    }
    public class Users
    {
        public int UserId { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
    }

}
