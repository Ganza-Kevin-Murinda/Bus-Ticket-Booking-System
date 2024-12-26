using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class ManageBussesModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public ManageBussesModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            // Initialize objects to avoid null issues
            Route = new Route();

            Bus = new Bus();

            Driver = new Driver();

        }

        //session --variables

        public int? Userid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        //Message --variables
        public string Message { get; set; } = "";
        public string MessageColor { get; set; } = "black";

        // Route --variables
        public List<Route> routeList { get; set; } = new List<Route>();

        [BindProperty]
        public Route? Route { get; set; }

        // Bus --variables
        public List<Bus> Busses { get; set; } = new List<Bus>();

        [BindProperty]
        public Bus? Bus { get; set; }

        //Driver --variables
        [BindProperty]
        public Driver? Driver { get; set; }

        public List<Driver> driverList { get; set; } = new List<Driver>();

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

            // Load routes
            LoadBus();

            RetrieveRoute();

            RetrieveDriver();
        }

        public IActionResult OnPostAdd()
        {
            Console.WriteLine("OnPostAdd called");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Invalid ModelState");
                Message = "Invalid data. Please fill in all fields.";
                MessageColor = "danger";
                return Page();
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();
                    Console.WriteLine("Database connection opened");

                    string query;
                    if (Request.Form["IsEdit"] == "true")
                    {
                        query = "UPDATE Bus SET nrOfSeats = @_2, model = @_3, routeId = @_4, " +
                                "driverId = @_5, busDeparture = @_6 , busArrival = @_7" +
                                "WHERE plateNr = @_1";
                    }
                    else
                    {
                        query = "INSERT INTO Bus (plateNr, nrOfSeats, model, routeId, driverId, busDeparture, busArrival) " +
                                "VALUES (@_1, @_2, @_3, @_4, @_5, @_6, @_7)";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@_1", Bus.PlateNo);
                        cmd.Parameters.AddWithValue("@_2", Bus.SeatNo);
                        cmd.Parameters.AddWithValue("@_3", Bus.Model);
                        cmd.Parameters.AddWithValue("@_4", Bus.RouteId);
                        cmd.Parameters.AddWithValue("@_5", Bus.DriverId);
                        // Ensure BusDeparture is properly parsed
                        DateTime busDeparture , busArrival;
                        if (DateTime.TryParse(Request.Form["Bus.BusDeparture"], out busDeparture) && DateTime.TryParse(Request.Form["Bus.BusArrival"], out busArrival))
                        {
                            cmd.Parameters.AddWithValue("@_6", busDeparture);
                            cmd.Parameters.AddWithValue("@_7", busArrival);
                        }
                        else
                        {
                            throw new Exception("Invalid date format.");
                        }

                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Query executed successfully");
                    }
                }

                Message = Request.Form["IsEdit"] == "true"
                    ? "Bus updated successfully!"
                    : "Bus created successfully!";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Message = "Error saving bus: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string plateNr)
        {
            Console.WriteLine("plate Nr:" + plateNr);
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = "DELETE FROM Bus WHERE plateNr = @plate";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@plate", plateNr);
                        cmd.ExecuteNonQuery();
                    }
                }

                Message = "Bus deleted successfully!";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Message = "Error deleting Bus: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
        }


        public void RetrieveRoute()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Route", conn))
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

        public void RetrieveDriver()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Drivers WHERE userId IN (SELECT userId FROM Users WHERE isActive = 1)", conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                Driver driver = new Driver();
                                driver.DriverId = reader.GetString("driverId");
                                driver.FullName = reader.GetString("name");


                                // add Route to the list
                                driverList.Add(driver);
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

        public void LoadBus()
        {

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = "SELECT * FROM Bus";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Busses.Add(new Bus
                                {
                                    PlateNo = reader.GetString(reader.GetOrdinal("plateNr")),
                                    Model = reader.GetString(reader.GetOrdinal("model")),
                                    SeatNo = reader.GetInt32(reader.GetOrdinal("nrOfSeats")),
                                    BusDeparture = reader.GetDateTime(reader.GetOrdinal("busDeparture")),
                                    BusArrival = reader.GetDateTime(reader.GetOrdinal("busArrival")),
                                    DriverId = reader.GetString(reader.GetOrdinal("driverId")),
                                    RouteId = reader.GetInt32(reader.GetOrdinal("routeId"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading Bus: " + ex.Message;
                MessageColor = "danger";
            }

        }

    }

    public class Bus
    {
        public string? PlateNo { get; set; }
        public string? Model { get; set; }
        public int SeatNo { get; set; }
        public DateTime BusDeparture { get; set; }
        public DateTime BusArrival { get; set; }
        public int RouteId { get; set; }
        public string? DriverId { get; set; }
        public Route? Route { get; set; }
    }

}
