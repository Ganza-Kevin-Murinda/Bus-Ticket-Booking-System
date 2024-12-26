using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class SigninModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string? connString = null;

        public SigninModel(IConfiguration configuration)
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
        public void OnPost()
        {
            RetrieveUser();
        }

        public void RetrieveUser()
        {
            if (string.IsNullOrEmpty(Request.Form["Email"]) || string.IsNullOrEmpty(Request.Form["PasswordHash"]))
            {
                Message = "All fields are required.";
                MessageColor = "danger";
                return;
            }

            string email = Request.Form["Email"];
            string plainPassword = Request.Form["PasswordHash"];

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    string query = "SELECT * FROM Users WHERE email = @_2";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@_2", email);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                theUsers.UserId = reader.GetInt32(reader.GetOrdinal("userId"));
                                theUsers.Email = reader.GetString(reader.GetOrdinal("email"));
                                theUsers.PasswordHash = reader.GetString(reader.GetOrdinal("password"));
                                theUsers.Role = reader.GetString(reader.GetOrdinal("role"));
                                theUsers.IsActive = reader.GetBoolean(reader.GetOrdinal("isActive"));

                                if (BCrypt.Net.BCrypt.Verify(plainPassword, theUsers.PasswordHash))
                                {
                                    if (theUsers.IsActive)
                                    {
                                        // Store session values
                                        HttpContext.Session.SetInt32("userId", theUsers.UserId);
                                        HttpContext.Session.SetString("role", theUsers.Role);

                                        string userName = GetUserName(theUsers.Role, theUsers.UserId);
                                        HttpContext.Session.SetString("userName", userName);

                                        // Debug log
                                        Console.WriteLine($"Login Successful: UserId: {theUsers.UserId}, Role: {theUsers.Role}, Username: {userName}");

                                        // Redirect based on role
                                        Response.Redirect(theUsers.Role.Equals("ADMIN") ? "/AdminDashboard" :
                                                          theUsers.Role.Equals("DRIVER") ? "/DriverDashboard" : "/ClientDashboard");
                                    }
                                    else
                                    {
                                        Message = "Account doesn't exist.";
                                        MessageColor = "danger";
                                    }
                                }
                                else
                                {
                                    Message = "Invalid username or password.";
                                    MessageColor = "danger";
                                }
                            }
                            else
                            {
                                Message = "No user found with the given email.";
                                MessageColor = "danger";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Message = ex.Message;
                MessageColor = "danger";
            }
        }


        // Helper method to retrieve the user's name based on their role
        private string GetUserName(string role, int userId)
        {
            string query = role switch
            {
                "ADMIN" => "SELECT Name FROM Admins WHERE UserId = @UserId",
                "DRIVER" => "SELECT Name FROM Drivers WHERE UserId = @UserId",
                "CUSTOMER" => "SELECT Name FROM Clients WHERE UserId = @UserId",
                _ => throw new Exception("Invalid role")
            };

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetString(reader.GetOrdinal("Name"));
                        }
                    }
                }
            }

            return "Unknown User";
        }


    }
}
