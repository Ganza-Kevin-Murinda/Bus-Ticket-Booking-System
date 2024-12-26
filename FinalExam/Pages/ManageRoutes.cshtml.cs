using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class ManageRoutesModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public ManageRoutesModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            // Initialize Route to avoid null issues
            Route = new Route();

            // Initialize Stop to avoid null issues
            Stop = new Stop();

            Bus = new Bus();
        }

        //session --variables

        public int? Userid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        //Message --variables
        public string Message { get; set; } = "";
        public string MessageColor { get; set; } = "black";

        // Route --variables
        public List<Route> Routes { get; set; } = new List<Route>();

        [BindProperty]
        public Route? Route { get; set; }

        public Bus Bus { get; set; }

        //Stops --variables

        [BindProperty]
        public List<Stop> Stops { get; set; } = new();

        [BindProperty]
        public Route CurrentRoute { get; set; } = new();

        [BindProperty]
        public bool IsManageStops { get; set; } = false;

        [BindProperty]
        public Stop Stop { get; set; }

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
            LoadRoutes();
        }

        public IActionResult OnPostSave()
        {
            Console.WriteLine("OnPostSave called");

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

                    string query = Route.RouteId == 0
                        ? "INSERT INTO Route (fromDeparture, toDestination) VALUES (@FromDeparture, @ToDestination)"
                        : "UPDATE Route SET fromDeparture = @FromDeparture, toDestination = @ToDestination WHERE routeId = @RouteId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDeparture", Route.FromDeparture);
                        cmd.Parameters.AddWithValue("@ToDestination", Route.ToDestination);

                        if (Route.RouteId != 0)
                        {
                            cmd.Parameters.AddWithValue("@RouteId", Route.RouteId);
                        }

                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Query executed successfully");
                    }
                }

                Message = Route.RouteId == 0
                    ? "Route created successfully!"
                    : "Route updated successfully!";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Message = "Error saving route: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
        }



        public IActionResult OnPostDelete(int routeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = "DELETE FROM Route WHERE routeId = @RouteId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RouteId", routeId);
                        cmd.ExecuteNonQuery();
                    }
                }

                Message = "Route deleted successfully!";
                MessageColor = "success";
            }
            catch (Exception ex)
            {
                Message = "Error deleting route: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage();
        }


        private void LoadRoutes()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = "SELECT * FROM Route ORDER BY routeId ASC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Routes.Add(new Route
                                {
                                    RouteId = reader.GetInt32(reader.GetOrdinal("routeId")),
                                    FromDeparture = reader.GetString(reader.GetOrdinal("fromDeparture")),
                                    ToDestination = reader.GetString(reader.GetOrdinal("toDestination"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading routes: " + ex.Message;
                MessageColor = "danger";
            }
        }



        public IActionResult OnPostManageStops(int routeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    // Get Route Info
                    string routeQuery = "SELECT routeId, fromDeparture, toDestination FROM Route WHERE routeId = @RouteId";
                    using (SqlCommand cmd = new SqlCommand(routeQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@RouteId", routeId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                CurrentRoute.RouteId = reader.GetInt32(0);
                                CurrentRoute.FromDeparture = reader.GetString(1);
                                CurrentRoute.ToDestination = reader.GetString(2);
                            }
                        }
                    }

                    // Get Stops for the Route
                    string stopsQuery = "SELECT stopId, stopName FROM Stops WHERE routeId = @RouteId";
                    using (SqlCommand cmd = new SqlCommand(stopsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@RouteId", routeId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Stops.Add(new Stop
                                {
                                    StopId = reader.GetInt32(0),
                                    StopName = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                IsManageStops = true; // Open modal
            }
            catch (Exception ex)
            {
                // Handle exception (log or display error)
                Message = "Error loading routes: " + ex.Message;
                MessageColor = "danger";
            }

            return Page();
        }

        public IActionResult OnPostSaveStop()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = "INSERT INTO Stops (routeId, stopName) VALUES (@RouteId, @StopName)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RouteId", Stop.RouteId);
                        cmd.Parameters.AddWithValue("@StopName", Stop.StopName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or display error)
                Message = "Error loading routes: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage(new { routeId = Stop.RouteId, handler = "ManageStops" });
        }

        public IActionResult OnPostDeleteStop(int stopId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = "DELETE FROM Stops WHERE stopId = @StopId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StopId", stopId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or display error)
                Message = "Error loading routes: " + ex.Message;
                MessageColor = "danger";
            }

            return RedirectToPage(new { handler = "ManageStops" });
        }

        public IActionResult OnPostCloseModal()
        {
            IsManageStops = false; // Close modal
            return Redirect($"/ManageRoutes");
        }


    }

    public class Route
    {
        public int RouteId { get; set; }
        public string? FromDeparture { get; set; }
        public string? ToDestination { get; set; }
    }

    public class Stop
    {
        public int StopId { get; set; }
        public int RouteId { get; set; }
        public string? StopName { get; set; }
    }

}
