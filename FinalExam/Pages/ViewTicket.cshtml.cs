using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class ViewTicketModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public ViewTicketModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            Ticket = new Ticket();
        }

        // Session variables
        public int Userid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        // Ticket variables
        [BindProperty]
        public Ticket? Ticket { get; set; }

        // Message variables
        public string Message { get; set; } = "";
        public string MessageColor { get; set; } = "black";

        public void OnGet()
        {
            // Retrieve session values
            Username = HttpContext.Session.GetString("userName");
            Role = HttpContext.Session.GetString("role");
            Userid = HttpContext.Session.GetInt32("userId") ?? -1;

            if (Userid == -1 || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Role))
            {
                Response.Redirect("/Signin");
                return;
            }

            // Load ticket details
            LoadTicketDetails();
        }

        public void LoadTicketDetails()
        {
            try
            {
                int clientId = SelectClientId();

                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    string query = @"
                        SELECT TOP 1 t.ticketId, t.busId, t.routeDeparture, t.routeDestination, 
                                      t.departureDate, t.price, t.paymentMethod, t.isCancel
                        FROM Ticket t
                        WHERE t.clientId = @ClientId
                        ORDER BY t.departureDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ClientId", clientId);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Ticket = new Ticket
                                {
                                    TicketId = reader.GetInt32("ticketId"),
                                    BusId = reader.GetString("busId"),
                                    RouteDeparture = reader.GetString("routeDeparture"),
                                    RouteDestination = reader.GetString("routeDestination"),
                                    DepartureDate = reader.GetDateTime("departureDate"),
                                    Price = reader.GetString("price"),
                                    PaymentMethod = reader.GetString("paymentMethod"),
                                    isCancel = reader.GetBoolean("isCancel")
                                };
                            }
                            else
                            {
                                Message = "No ticket found.";
                                MessageColor = "danger";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                MessageColor = "danger";
            }
        }

        public int SelectClientId()
        {
            int? sessionUserId = HttpContext.Session.GetInt32("userId");
            if (sessionUserId == null)
            {
                Response.Redirect("/Signin");
                return -1;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    string query = "SELECT clientId FROM Clients WHERE userId = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", sessionUserId.Value);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader.GetInt32("clientId");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading clientId: {ex.Message}");
            }

            return -1;
        }

        public IActionResult OnPostCancel()
        {
            try
            {
                int ticketId = Ticket.TicketId; // Assuming Ticket is bound from the form

                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    // Retrieve the ticket purchase time
                    string getPurchaseTimeQuery = "SELECT departuredate FROM Ticket WHERE ticketId = @TicketId";
                    DateTime purchaseTime;

                    using (SqlCommand cmd = new SqlCommand(getPurchaseTimeQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TicketId", ticketId);
                        object result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            Message = "Ticket not found.";
                            MessageColor = "red";
                            return RedirectToPage();
                        }

                        purchaseTime = Convert.ToDateTime(result);
                    }

                    // Check if more than 1 hour has passed since the ticket was purchased
                    if ((DateTime.Now - purchaseTime).TotalHours > 1)
                    {
                        Message = "You cannot cancel this ticket as it was purchased more than an hour ago.";
                        MessageColor = "red";
                        return RedirectToPage();
                    }

                    // Proceed to cancel the ticket
                    string cancelQuery = "UPDATE Ticket SET isCancel = 1 WHERE ticketId = @TicketId";
                    using (SqlCommand cmd = new SqlCommand(cancelQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TicketId", ticketId);
                        cmd.ExecuteNonQuery();
                    }
                }

                Message = "Ticket canceled successfully.";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error canceling ticket: " + ex.Message);
                Message = "Error canceling ticket.";
                MessageColor = "red";
            }

            return RedirectToPage();
        }


    }
}
