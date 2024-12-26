using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Numerics;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FinalExam.Pages
{
    public class ManageDriversModel : PageModel
    {

        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public ManageDriversModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            // Initialize Driver to avoid null issues
            Driver = new Driver();

            User = new Users();
        }

        //session --variables

        public int? Userid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        //Message --variables
        public string Message { get; set; } = "";
        public string MessageColor { get; set; } = "black";

        //Driver --variables
        [BindProperty]
        public Driver? Driver { get; set; }

        public List<Driver> Drivers { get; set; } = new List<Driver>();

        //User --object
        [BindProperty]
        public new Users? User { get; set; }

        public void OnGet()
        {
            // Retrieve session values
            Username = HttpContext.Session.GetString("userName");
            Userid = HttpContext.Session.GetInt32("userId");
            Role = HttpContext.Session.GetString("role");

            // Redirect to login if session is invalid
            if (Userid == null || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Role))
            {
                Response.Redirect("/Signin");
                return;
            }

            // Load Drivers
            LoadDrivers();
        }

        public IActionResult OnPostAdd()
        {
            Console.WriteLine("OnPostAdd called");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid.");
                foreach (var state in ModelState)
                {
                    Console.WriteLine($"Key: {state.Key}, Error: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return Page();
            }


            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();
                    Console.WriteLine("Database connection opened");

                    // Hash the password
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(User.PasswordHash);

                    Console.WriteLine("hashed password:" + hashedPassword);

                    // Generate OTP
                    string otp = new Random().Next(100000, 999999).ToString(); // Random 6-digit OTP

                    string role = "DRIVER";

                    // First Insert into Users table
                    string insertUserQuery = @"
                        INSERT INTO Users (email, password, OTP, isActive, role)
                        OUTPUT INSERTED.userId
                        VALUES (@Email, @PasswordHash, @OTP, @IsActive, @Role)";
                    using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", User.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        cmd.Parameters.AddWithValue("@OTP", otp);
                        cmd.Parameters.AddWithValue("@IsActive", false);
                        cmd.Parameters.AddWithValue("@Role", role);

                        // Retrieve UserId from the OUTPUT clause
                        int userId = (int)cmd.ExecuteScalar();

                        Console.WriteLine("User ID:" + userId);

                        // Insert into Drivers table
                        string insertDriverQuery = @"
                            INSERT INTO Drivers (driverId,name, license, phone, userId)
                            VALUES (@DId,@Name, @DLicense, @Phone, @UserId)";
                        using (SqlCommand cmd1 = new SqlCommand(insertDriverQuery, conn))
                        {
                            cmd1.Parameters.AddWithValue("@DId", Driver.DriverId);
                            cmd1.Parameters.AddWithValue("@Name", Driver.FullName);
                            cmd1.Parameters.AddWithValue("@DLicense", Driver.DriverLicense);
                            cmd1.Parameters.AddWithValue("@Phone", Driver.DriverPhone);
                            cmd1.Parameters.AddWithValue("@UserId", userId);

                            cmd1.ExecuteNonQuery();
                        }

                        // Send OTP via email
                        SendOtpEmail(User.Email, otp);

                        // Log redirection
                        Console.WriteLine($"Redirecting to OTP page for Email: {User.Email}");
                        return Redirect($"/OTP?email={User.Email}&redirect=/ManageDrivers");

                    }

                }

                Message = "Driver created successfully!";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Message = "Error saving driver: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int userId)
        {
            Console.WriteLine("user:" +  userId);
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = @"UPDATE Users SET isActive = @active WHERE userId = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", userId);
                        cmd.Parameters.AddWithValue("@active", false);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            Console.WriteLine("No rows were updated. Please verify the userId or isActive field.");
                        }
                        else
                        {
                            Console.WriteLine($"Rows updated: {rowsAffected}");
                        }
                    }

                    conn.Close();
                }

                Message = "Driver Account Disactivated successfully!";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Message = "Error deleting Driver: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
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

        private void LoadDrivers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = @"SELECT * FROM Drivers WHERE userId IN (SELECT userId FROM Users WHERE isActive = 1) ORDER BY driverId ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Drivers.Add(new Driver
                                {
                                    DriverId = reader.GetString(reader.GetOrdinal("driverId")),
                                    FullName = reader.GetString(reader.GetOrdinal("name")),
                                    DriverLicense = reader.GetString(reader.GetOrdinal("license")),
                                    DriverPhone = reader.GetString(reader.GetOrdinal("phone")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("userId"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading Drivers: " + ex.Message;
                MessageColor = "danger";
            }
        }

    }

    public class Driver
    {
        public string? DriverId { get; set; }
        public string? FullName { get; set; }
        public string? DriverLicense { get; set; }
        public string? DriverPhone { get; set; }
        public int UserId { get; set; }
    }

}
