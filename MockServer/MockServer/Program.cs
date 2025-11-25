using Microsoft.AspNetCore.Mvc;

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

// 1. Mock for Navigation Module (for create_new_trip)
app.MapPost("/api/estimate", ([FromBody] object payload) =>
{
    Console.WriteLine("Mock /api/estimate was called.");
    Console.WriteLine($"Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
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

// 2. Mock for Auth Module (for create_new_trip)
app.MapPost("/api/authentication/create_new_trip", ([FromBody] object payload) =>
{
    Console.WriteLine("Mock /api/authentication/create_new_trip was called.");
    Console.WriteLine($"Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
    var mockAuthResponse = new
    {
        ride_id = 12345, // Return a mock ride ID
        status = "PENDING"
    };
    return Results.Ok(mockAuthResponse);
})
.WithName("MockAuthCreateTrip");

// 3. UPDATED Mock for Driver Module (for create_new_trip)
// Your code updated driverResponse to only contain driver_id
app.MapPost("/api/driver/assign_driver", ([FromBody] object payload) =>
{
    Console.WriteLine("Mock /api/driver/assign_driver was called.");
    Console.WriteLine($"Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
    var mockDriverResponse = new
    {
        driver_id = 999 // Updated to return only ID as per your new record definition
    };
    return Results.Ok(mockDriverResponse);
})
.WithName("MockDriverAssign");

// 4. Mock for Auth 'me' endpoint (from your AuthService)
app.MapGet("/me", (HttpContext context) =>
{
    Console.WriteLine("Mock /me was called.");
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        // Optional: Relax this check for testing if needed, or provide a default token in your client
        // return Results.Unauthorized(); 
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

// 5. Mock for Payment Module (for confirm_trip)
app.MapPost("/api/payments", ([FromBody] object paymentRequest) =>
{
    Console.WriteLine($"Mock /api/payments was called.");
    Console.WriteLine($"Payment Request Payload: {System.Text.Json.JsonSerializer.Serialize(paymentRequest)}");

    return Results.Ok(new
    {
        transaction_id = $"mock_txn_{Guid.NewGuid()}",
        status = "successful"
    });
})
.WithName("MockPayment");


// 6. Mock for Navigation/Driver Module (for driver_location)
app.MapGet("/lastLocation", (string driverID) =>
{
    Console.WriteLine($"Mock /lastLocation was called for driverID: {driverID}");

    var mockLocation = new
    {
        longitude = "12.1243",
        latitude = "14.2323"
    };

    return Results.Ok(mockLocation);
})
.WithName("MockDriverLocation");

// 7. Mock for Driver Manager (for finish_ride)
app.MapPost("/api/DriverManager/DriverComplete", ([FromBody] object finishRidePayload) =>
{
    Console.WriteLine($"Mock /api/DriverManager/DriverComplete was called.");
    Console.WriteLine($"Finish Ride Payload: {System.Text.Json.JsonSerializer.Serialize(finishRidePayload)}");

    return Results.Ok(new { status = "ride_completed", driver_rating_received = true });
})
.WithName("MockDriverComplete");

// 8. NEW MOCK: Geocoding (for create_new_trip)
app.MapGet("/api/geocode", (string query) =>
{
    Console.WriteLine($"Mock /api/geocode was called for query: {query}");

    // Return dummy coordinates
    // Matches your 'Location' record: public double latitude; public double longitude;
    var mockLocation = new
    {
        latitude = 43.4643,
        longitude = -80.5204
    };
    return Results.Ok(mockLocation);
})
.WithName("MockGeocode");

// 9. NEW MOCK: Get Driver Info (for create_new_trip)
app.MapPost("/api/authentication/get_driver_info", ([FromBody] object payload) =>
{
    Console.WriteLine("Mock /api/authentication/get_driver_info was called.");
    Console.WriteLine($"Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");

    // Matches your 'DriverInfo' record
    var mockDriverInfo = new
    {
        driver_id = 999,
        driver_name = "David James",
        license_plate = "LICE NSEPLATE",
        car_model = "Honda CRV"
    };
    return Results.Ok(mockDriverInfo);
})
.WithName("MockGetDriverInfo");

app.Run();
