using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class RouteDetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string? ConnString { get; }

        public RouteDetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            ConnString = _configuration.GetConnectionString("MyConn");

            Bus = new Bus();
            Route = new Route();
            Stops = new List<Stop>();
        }

        // Session Variables
        public int? Userid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }

        // Message Variables
        public string Message { get; set; } = "";
        public string MessageColor { get; set; } = "black";

        // Bus, Route, and Stops
        public Bus Bus { get; set; }
        public Route Route { get; set; }
        public List<Stop> Stops { get; set; }

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

            // Load data for the driver's bus, route, and stops
            LoadDriverBusRouteStops();
        }

        private void LoadDriverBusRouteStops()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    // Fetch the bus the driver is assigned to
                    string busQuery = @"
                        SELECT b.plateNr, b.model, b.busDeparture, b.busArrival, r.routeId, r.fromDeparture, r.toDestination
                        FROM Bus b
                        INNER JOIN Route r ON b.routeId = r.routeId
                        WHERE b.driverId IN ( select driverId from Drivers where userId = @userId)";

                    using (SqlCommand cmd = new SqlCommand(busQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", Userid);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Bus = new Bus
                                {
                                    PlateNo = reader.GetString(reader.GetOrdinal("plateNr")),
                                    Model = reader.GetString(reader.GetOrdinal("model")),
                                    BusDeparture = reader.GetDateTime(reader.GetOrdinal("busDeparture")),
                                    BusArrival = reader.GetDateTime(reader.GetOrdinal("busArrival"))
                                };

                                Route = new Route
                                {
                                    RouteId = reader.GetInt32(reader.GetOrdinal("routeId")),
                                    FromDeparture = reader.GetString(reader.GetOrdinal("fromDeparture")),
                                    ToDestination = reader.GetString(reader.GetOrdinal("toDestination"))
                                };
                            }
                        }
                    }

                    // Fetch the stops associated with the route
                    string stopsQuery = "SELECT stopId, stopName FROM Stops WHERE routeId = @RouteId";
                    using (SqlCommand cmd = new SqlCommand(stopsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@RouteId", Route.RouteId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Stops.Add(new Stop
                                {
                                    StopId = reader.GetInt32(reader.GetOrdinal("stopId")),
                                    StopName = reader.GetString(reader.GetOrdinal("stopName"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading data: " + ex.Message;
                MessageColor = "danger";
            }
        }
    }
}
