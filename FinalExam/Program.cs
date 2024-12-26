var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true;                // Secure cookie
    options.Cookie.IsEssential = true;             // Required for GDPR compliance
    options.Cookie.SameSite = SameSiteMode.Strict; // Prevent cross-site session sharing
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensure HTTPS for cookies
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow insecure cookies in development
    options.Cookie.SameSite = SameSiteMode.Lax;
});


// Add support for configuration or other services
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable session middleware
app.UseSession();

// Add this line before `app.UseRouting()`
app.UseCors("AllowAll");


app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
