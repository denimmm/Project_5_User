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

// 1. Mock for Navigation Module: /api/estimate
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

// 3. Mock for Driver Module (for create_new_trip)
app.MapPost("/api/driver/assign_driver", ([FromBody] object payload) =>
{
    Console.WriteLine("Mock /api/driver/assign_driver was called.");
    Console.WriteLine($"Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
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

// 5. NEW MOCK for Payment Module (for confirm_trip)
// Your main API calls: 
// ...PostAsJsonAsync("https://portainer.gooberapp.org:3456/api/payments", paymentRequest);
app.MapPost("/api/payments", ([FromBody] object paymentRequest) => // We can just accept an 'object' to be simple
{
    Console.WriteLine($"Mock /api/payments was called.");
    // Log the request payload to see if it's correct
    Console.WriteLine($"Payment Request Payload: {System.Text.Json.JsonSerializer.Serialize(paymentRequest)}");

    // Your code checks for IsSuccessStatusCode and 402.
    // Let's simulate a success case.
    return Results.Ok(new
    {
        transaction_id = $"mock_txn_{Guid.NewGuid()}",
        status = "successful"
    });

    // To test your 402 error path, you could comment the line above
    // and uncomment this one:
    // Console.WriteLine("Simulating payment failure (402).");
    // return Results.StatusCode(402); 
})
.WithName("MockPayment");


// 6. NEW MOCK for Navigation/Driver Module (for driver_location)
// Your main API calls: 
// ...client.GetAsync($"https://portainer.gooberapp.org:{port}/lastLocation?driverID={driverID}");
app.MapGet("/lastLocation", (string driverID) =>
{
    Console.WriteLine($"Mock /lastLocation was called for driverID: {driverID}");

    // Return the exact JSON structure your main API expects
    var mockLocation = new
    {
        longitude = "12.1243",
        latitude = "14.2323"
    };

    return Results.Ok(mockLocation);
})
.WithName("MockDriverLocation");

// 7. NEW MOCK for Driver Manager (for finish_ride)
// Your main API calls: 
// ...client.PostAsJsonAsync("https://localhost:7126/api/DriverManager/DriverComplete", finish_ride_payload);
app.MapPost("/api/DriverManager/DriverComplete", ([FromBody] object finishRidePayload) =>
{
    Console.WriteLine($"Mock /api/DriverManager/DriverComplete was called.");
    // Log the payload to make sure it's what you expect
    Console.WriteLine($"Finish Ride Payload: {System.Text.Json.JsonSerializer.Serialize(finishRidePayload)}");

    // Your /finish_ride endpoint just expects a 202 Accepted, 
    // so we just need to return a successful status.
    // We can return 200 OK or 204 NoContent, both will count as 'IsSuccessStatusCode'
    return Results.Ok(new { status = "ride_completed", driver_rating_received = true });
})
.WithName("MockDriverComplete");

app.Run();
