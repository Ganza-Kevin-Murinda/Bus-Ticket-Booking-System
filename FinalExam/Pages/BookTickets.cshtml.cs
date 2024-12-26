using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;

namespace FinalExam.Pages
{
    public class BookTicketsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public BookTicketsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            // Initialize objects to avoid null issues

            Route = new Route();

            Bus = new Bus();

            Ticket = new Ticket();

        }

        //session --variables

        public int Userid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        //Message --variables
        public string Message { get; set; } = "";
        public string MessageColor { get; set; } = "black";

        // Route --variables
        public List<Route> routeList { get; set; } = new List<Route>();

        public List<Client> Clients { get; set; } = new List<Client>();

        [BindProperty]
        public Route? Route { get; set; }

        // Bus --variables
        public List<Bus> busList { get; set; } = new List<Bus>();

        [BindProperty]
        public Bus? Bus { get; set; }

        // Ticket --variables
        [BindProperty]
        public Ticket? Ticket { get; set; }

        [BindProperty]
        public string? name { get; set; }

        [BindProperty]
        public string? phone { get; set; }

        public void OnGet()
        {
            // Retrieve session values safely
            Username = HttpContext.Session.GetString("userName");
            Role = HttpContext.Session.GetString("role");
            Userid = HttpContext.Session.GetInt32("userId") ?? -1; // Default to -1 if session value is null

            // Debug logs for session values
            Console.WriteLine($"Session Values - UserId: {Userid}, Username: {Username}, Role: {Role}");

            // Redirect to login if session is invalid
            if (Userid == -1 || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Role))
            {
                Console.WriteLine("Invalid session. Redirecting to login.");
                Response.Redirect("/Signin");
                return;
            }

            // Proceed to retrieve additional data
            RetrieveRoute();
            RetrieveBus();

            // Debug log route count
            Console.WriteLine($"Routes Count: {routeList.Count}");
        }


        public void RetrieveRoute()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT Route.routeId, Route.fromDeparture, Route.toDestination FROM Route JOIN Bus ON Bus.routeId = Route.routeId", conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                Route route = new Route();
                                route.RouteId = reader.GetInt32("routeId");
                                route.FromDeparture = reader.GetString("fromDeparture");
                                route.ToDestination = reader.GetString("toDestination");


                                // add Route to the list
                                routeList.Add(route);
                            }
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }

        public void RetrieveBus()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Bus", conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                Bus bus = new Bus();
                                bus.PlateNo = reader.GetString("plateNr");
                                bus.Model = reader.GetString("model");
                                bus.BusDeparture = reader.GetDateTime("busDeparture");
                                bus.BusArrival = reader.GetDateTime("busArrival");


                                // add Route to the list
                                busList.Add(bus);
                            }
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }

        public int SelectClientId()
        {
            // Retrieve Userid from session
            int? sessionUserId = HttpContext.Session.GetInt32("userId");
            if (sessionUserId == null)
            {
                Console.WriteLine("UserId not found in session. Redirecting to Signin.");
                Response.Redirect("/Signin");
                return -1; // Return an invalid ID
            }

            int userId = sessionUserId.Value;

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();
                    string query = @"SELECT clientId FROM Clients WHERE userId = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader.GetInt32(reader.GetOrdinal("clientId"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading clientId: {ex.Message}");
            }

            Console.WriteLine("Client ID not found or an error occurred.");
            return -1; // Return an invalid ID
        }


        public IActionResult OnPostBook()
        {
            name = Request.Form["fullName"];

            phone = Request.Form["contact"];

            Console.WriteLine("OnPostBook called");
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();
                    Console.WriteLine("Database connection opened");

                    // Retrieve ClientId using SelectClientId

                    int clientId = SelectClientId();
                    if (clientId == -1)
                    {
                        Console.WriteLine("Client ID retrieval failed. Redirecting to Signin.");
                        Response.Redirect("/Signin");
                        return Page();
                    }

                    // Retrieve form data
                    string fullName = Request.Form["FullName"];
                    string contact = Request.Form["Contact"];
                    string email = Request.Form["Email"];
                    string departureDate = Request.Form["Ticket.DepartureDate"];
                    string arrivalDate = Request.Form["Ticket.ArrivalDate"];
                    string busName = Request.Form["Ticket.BusId"];
                    string busModel = GetBusModel(busName); // Fetch the bus model from the database or logic
                    string departure = Request.Form["Ticket.RouteDeparture"];
                    string destination = Request.Form["Ticket.RouteDestination"];
                    string price = Request.Form["Ticket.Price"];
                    string paymentMethod = Request.Form["Ticket.PaymentMethod"];

                    Console.WriteLine($"Booking for Name: {fullName}, Phone: {contact}, ClientId: {clientId}");


                    string query;

                        query = "INSERT INTO Ticket (busId, clientId, routeDestination, routeDeparture, arrivalDate , departureDate, price, paymentMethod) " +
                                "VALUES (@_2, @_3, @_4, @_5, @_6, @_7, @_8,@_9)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@_2", Ticket?.BusId);
                            cmd.Parameters.AddWithValue("@_3", clientId);
                            cmd.Parameters.AddWithValue("@_4", Ticket?.RouteDestination);
                            cmd.Parameters.AddWithValue("@_5", Ticket?.RouteDeparture);
                            cmd.Parameters.AddWithValue("@_8", Ticket?.Price);
                            cmd.Parameters.AddWithValue("@_9", Ticket?.PaymentMethod);
                            // Ensure BusDeparture is properly parsed
                            DateTime departureDates, arrivalDates;
                            if (DateTime.TryParse(Request.Form["Ticket.DepartureDate"], out departureDates) && DateTime.TryParse(Request.Form["Ticket.ArrivalDate"], out arrivalDates))
                            {
                                cmd.Parameters.AddWithValue("@_6", departureDates);
                                cmd.Parameters.AddWithValue("@_7", arrivalDates);
                        }
                            else
                            {
                                throw new Exception("Invalid date format.");
                            }

                            cmd.ExecuteNonQuery();
                            Console.WriteLine("Query executed successfully");
                        }

                        // Send ticket email
                        SendTicketEmail(email, fullName, contact, departureDate, arrivalDate, busName, busModel, departure, destination, price, paymentMethod);

                    Message = "Booking and email successful.";
                    MessageColor = "success";

                    Console.WriteLine("Booking and email successful.");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Message = "Error saving bus: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
        }

        public string GetBusModel(string busId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    string query = "SELECT Model FROM Bus WHERE PlateNo = @BusId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BusId", busId);
                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        return result != null ? result.ToString() : "Unknown Model";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching bus model: {ex.Message}");
                return "Unknown Model";
            }
        }


        public void SendTicketEmail( string email,string fullName,string contact,string travelDate, string arrivalDate, string busName,
    string busModel,string departure,string destination,string price,string paymentMethod)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("linuxkevin2024@gmail.com");
                mail.To.Add(email);
                mail.Subject = "Your Bus Ticket Booking Confirmation";

                mail.Body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{
                                font-family: Arial, sans-serif;
                                line-height: 1.6;
                            }}
                            .ticket-container {{
                                border: 1px solid #ccc;
                                padding: 20px;
                                max-width: 600px;
                                margin: auto;
                            }}
                            .ticket-header {{
                                text-align: center;
                                margin-bottom: 20px;
                            }}
                            .ticket-details {{
                                margin-bottom: 10px;
                            }}
                            .ticket-details label {{
                                font-weight: bold;
                            }}
                            .footer {{
                                text-align: center;
                                font-size: 0.9em;
                                color: #555;
                                margin-top: 20px;
                            }}
                        </style>
                    </head>
                    <body>
                        <div class='ticket-container'>
                            <div class='ticket-header'>
                                <h2>Bus Ticket Booking Confirmation</h2>
                                <p>Thank you for booking with us, {fullName}!</p>
                            </div>
                            <div class='ticket-details'>
                                <p><label>Full Name:</label> {fullName}</p>
                                <p><label>Contact:</label> {contact}</p>
                                <p><label>Travel Date:</label> {travelDate}</p>
                                <p><label>Travel Date:</label> {arrivalDate}</p>
                                <p><label>Bus Name:</label> {busName}</p>
                                <p><label>Bus Model:</label> {busModel}</p>
                                <p><label>Departure:</label> {departure}</p>
                                <p><label>Destination:</label> {destination}</p>
                                <p><label>Ticket Price:</label> {price}Rwf</p>
                                <p><label>Payment Method:</label> {paymentMethod}</p>
                            </div>
                            <div class='footer'>
                                <p>Please keep this email as a reference for your journey.</p>
                                <p>Safe travels!</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                mail.IsBodyHtml = true;

                smtpServer.Port = 587;
                smtpServer.Credentials = new System.Net.NetworkCredential("linuxkevin2024@gmail.com", "ysqqnkufjckksxjf");
                smtpServer.EnableSsl = true;

                smtpServer.Send(mail);

                Console.WriteLine("Ticket email sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }



    }

    public class Ticket
    {
        public int TicketId { get; set; }
        public string? BusId { get; set; }
        public int ClientId { get; set; }
        public string? RouteDestination { get; set; }
        public string? RouteDeparture { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public DateTime? DepartureDate { get; set; }
        public string? Price { get; set; }
        public string? PaymentMethod { get; set; }
        public bool? isCancel { get; set; }
    }

}
