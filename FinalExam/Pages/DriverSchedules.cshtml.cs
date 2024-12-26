using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;

namespace FinalExam.Pages
{
    public class DriverSchedulesModel : PageModel
    {
            private readonly IConfiguration _configuration;
            public string? ConnString { get; }

            public DriverSchedulesModel(IConfiguration configuration)
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

        }
        public void LoadBus()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    Bus.plateNr, 
                    Bus.model, 
                    Bus.nrOfSeats, 
                    Bus.busDeparture,
                    Bus.busArrival,
                    Route.fromDeparture, 
                    Route.toDestination
                FROM 
                    Bus
                JOIN 
                    Route 
                ON 
                    Bus.routeId = Route.routeId";

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
                                    Route = new Route
                                    {
                                        FromDeparture = reader.GetString(reader.GetOrdinal("fromDeparture")),
                                        ToDestination = reader.GetString(reader.GetOrdinal("toDestination"))
                                    }
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

    }
}
