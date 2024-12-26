using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class ManageClientsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public ManageClientsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            // Initialize Client to avoid null issues
            Client = new Client();
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
        public Client? Client { get; set; }

        public List<Client> Clients { get; set; } = new List<Client>();

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

            // Load Clients
            LoadClients();
        }

        private void LoadClients()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = @"SELECT * FROM Clients WHERE userId IN (SELECT userId FROM Users WHERE isActive = 1) ORDER BY clientId ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Clients.Add(new Client
                                {
                                    ClientId = reader.GetInt32(reader.GetOrdinal("clientId")),
                                    FullName = reader.GetString(reader.GetOrdinal("name")),
                                    ClientPhone = reader.GetString(reader.GetOrdinal("phoneNo")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("userId"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading Client: " + ex.Message;
                MessageColor = "danger";
            }
        }

        public IActionResult OnPostDelete(int userId)
        {
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
    }
    public class Client
    {
        public int ClientId { get; set; }
        public string? FullName { get; set; }
        public string? ClientPhone { get; set; }
        public int UserId { get; set; }
    }
}
