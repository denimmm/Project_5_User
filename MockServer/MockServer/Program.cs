var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

Console.WriteLine("Mock Services API is running...");
Console.WriteLine("Note: You may need to trust the .NET development certificate (`dotnet dev-certs https --trust`)");

// --- MOCK ENDPOINTS ---

// 1. Mock for Navigation Module: /api/estimate
app.MapPost("/api/estimate", () =>
{
    Console.WriteLine("Mock /api/estimate was called.");
    // Return the hard-coded response your main app expects
    var mockNavResponse = new
    {
        distanceKM = 14.58,
        fare = 29.04,
        durationMinutes = 1.86,
        polyline = "mock_polyline_string_goes_here"
    };
    return Results.Ok(mockNavResponse);
})
.WithName("MockNavEstimate");

// 2. Mock for Auth Module: /api/authentication/create_new_trip
app.MapPost("/api/authentication/create_new_trip", () =>
{
    Console.WriteLine("Mock /api/authentication/create_new_trip was called.");
    var mockAuthResponse = new
    {
        ride_id = 12345, // Return a mock ride ID
        status = "PENDING"
    };
    return Results.Ok(mockAuthResponse);
})
.WithName("MockAuthCreateTrip");

// 3. Mock for Driver Module: /api/driver/assign_driver
app.MapPost("/api/driver/assign_driver", () =>
{
    Console.WriteLine("Mock /api/driver/assign_driver was called.");
    var mockDriverResponse = new
    {
        driver_name = "Mock Matthew",
        license_plate = "MOCK 123",
        car_model = "Mock Biege Chevy Malibu"
    };
    return Results.Ok(mockDriverResponse);
})
.WithName("MockDriverAssign");

// 4. Mock for Auth 'me' endpoint (from your AuthService)
app.MapGet("/me", (HttpContext context) =>
{
    Console.WriteLine("Mock /me was called.");
    // Check if the auth header is present
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        return Results.Unauthorized();
    }

    var mockUser = new
    {
        account_id = "mock-user-123",
        username = "mockuser",
        email = "mock@user.com",
        role = "Rider"
    };
    return Results.Ok(mockUser);
})
.WithName("MockAuthMe");

// Add other mocks as needed (e.g., for payments, driver location)

app.Run();
