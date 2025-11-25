using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Writers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
//add service for auth
builder.Services.AddHttpClient("Authentication", client =>
{
    client.BaseAddress = new Uri("https://api.auth/manager");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<AuthService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



//GINBOT
//verify the authentication with authentication module

bool verifyAuth(String? auth_header, IHttpClientFactory httpClientFactory)
{
    //make sure string is not empty
    if (string.IsNullOrEmpty(auth_header))
        return false;


    //send to authentication module for verification
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7126/me");
    //add token to authentication header
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth_header);
    try
    {
        //send request
        var response = client.Send(request);
        if (response.IsSuccessStatusCode)
            return true;
        else
            return false;
    }
    catch
    {
        return false;
    }

}



//Maksym
//allows the user to request a ride and receive an offer. offer must be confirmed
// /api/create_new_trip
////input: POST { "userID": "u12345", "pickup_address": "Conestoga College, Waterloo, ON", "destination_address": "Conestoga Mall, Waterloo, ON", "car_type" : "XL", "pet_friendly" : "true"}
////output: { "rideID" : "01242", "distanceKm" : "14.58", "fare" : "29.04", "durationMinutes" : "1.86", "driver_name" : "Matthew", "license_plate" : "KJVM 719", "car_model" : "Biege Chevy Malibu"}
app.MapPost("/create_new_trip", async (RideRequest request, IHttpClientFactory httpClientFactory, HttpContext context, AuthService authService) =>
{
    //authenticate
    //verify the user's authentication token
    var authHeader = context.Request.Headers["Authorization"].ToString();
    authHeader = "djdjdjjjdj";
    //verify user with authservice
    //if (!(await authService.verifyAuth(authHeader)))
    //    return Results.BadRequest();
    //verify with function
    if (!verifyAuth(authHeader, httpClientFactory))
        return Results.BadRequest();
    //make http client to access navigation authentication and driver endpoints
    var client = httpClientFactory.CreateClient();

    //get estimate from navigation module
    var navInput = new
    {
        pickupAddress = request.pickup_address,
        destinationAddress = request.destination_address
    };

    //call navigation module /api/estimate
    var navEstimateResponse = await client.PostAsJsonAsync("https://localhost:7126/api/estimate", navInput);

    //populate nav estimate response object
    var navEstimateContent = await navEstimateResponse.Content.ReadFromJsonAsync<navEstimateResponse>();



    //get ride id from auth /create_new_trip
    var authRequestJson = new
    {
        rider_id = request.userID,
        pickup = new 
            { lattitude = 0,
              longitude = 0,
              address = request.pickup_address
            },
        dropOff = new
        {
            latitude = 0,
            longitude = 0,
            address = request.destination_address
        },
        car_type = request.car_type,
        pet_friendly = request.pet_friendly,
        estimate = new
        {
            distance_km =navEstimateContent.distanceKM,
            fare_estimate = navEstimateContent.fare,
            duration_min = navEstimateContent.durationMinutes
        }

    };

    //call auth module to create new trip
    var authResponse = await client.PostAsJsonAsync("https://localhost:7126/api/authentication/create_new_trip", authRequestJson);

    //Populate auth content object
    var authContent = await authResponse.Content.ReadFromJsonAsync<authResponse>();
    
    var DriverRequestJson = new
    {
        ride_id = authContent.ride_id
    };

    //call driver module to get assigned driver
    var driverResponse = await client.PostAsJsonAsync("https://localhost:7126/api/driver/assign_driver", DriverRequestJson);

    //get driver content
    var driverContent = await driverResponse.Content.ReadFromJsonAsync<driverResponse>();

    //return ride offer for confirmation

    var rideOffer = new
    {
        rideID = authContent.ride_id,
        distanceKm = navEstimateContent.distanceKM,
        fare = navEstimateContent.fare,
        durationMinutes = navEstimateContent.durationMinutes,
        driver_name = driverContent.driver_name,
        license_plate = driverContent.license_plate,
        car_model = driverContent.car_model,

    };

    return Results.Json(rideOffer);
})
.WithName("create_new_trip")
.WithOpenApi();


//MEEHAK
//confirms the ride for the user, activates payment, and dispatches a driver
// /api/confirm_trip
////input: { "userID" : "u12345", "rideID" : "01242", "confirm_ride" : "true" }
////output: { "rideID" : "12345", "driver_name" : "John", "ETA" : "17:40", "payment_successful" : "true" }

app.MapPost("/confirm_trip", async (HttpContext context, string tripID) =>
{
    //authenticate
    var authHeader = context.Request.Headers["Authorization"].ToString();
    //verifyAuth(authHeader);

    //Request body for Payments service
    var paymentRequest = new
    {
        tripID = tripID
    };
    using var httpClient = new HttpClient();
    //Send tripID to the Payments service 
    var paymentResponse = await httpClient.PostAsJsonAsync("https://localhost:7126/api/payments", paymentRequest);
    //check payments response status
    if (paymentResponse.IsSuccessStatusCode)
    {
        //forward the success code back to UX/UI team
        return Results.Ok(new
        {
            tripID = tripID,
            status = "Payment confirmed"
        });
    }
    //if payment gets failed 
    return Results.Problem("An error occured while processing payment.", statusCode: (int)paymentResponse.StatusCode);
})
.WithName("confirm_trip")
.WithOpenApi();


//Denim
//returns the location of the user's driver
// /api/driverLocation
////input: driverlocation?userID=12345&rideID=12312421
////output: {  "longitude" : "12.1243", "latitude" : "14.2323" }
app.MapGet("/driver_location", async (HttpContext context, int userID) =>
{

    //verify the user's authentication token
    var authHeader = context.Request.Headers["Authorization"].ToString();

    //verifyAuth(authHeader);


    //send auth token to auth module for verification. return unauthorized if invalid



    ////request driver location from the navigation or driver module
    //make http client to access navigation endpoint
    var client = new HttpClient();
    int port = 7126; //placeholder
    string driverID = "001"; //placeholder. will need to rettrieve this from the auth-data team or give it to the user in confirm_ride for the user to send back to us here.
    string navurl = $"https://localhost:{port}/lastLocation?driverID={driverID}";

    var response = await client.GetAsync(navurl);

    //error handling
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem("Failed to fetch driver location from navigation module");
    }

    //get the response
    var json = await response.Content.ReadAsStringAsync();

    //deserialize
    var navigationOutput = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);

    //return the output directly as it only contains the location, no other processing is required.
    return Results.Json(navigationOutput);
})
.WithName("GetDriverLocation")
.WithOpenApi();


//Denim or Maksym
//finish the ride and rate the driver
// /finishRide
////input: { UserID = 123324, RideID = 32492359, RideCompleted = true, Rating = 5 }
////output: 202 accepted
app.MapPost("/finish_ride", async (finishRide request, IHttpClientFactory httpClientFactory, HttpContext context) =>
{
    //authenticate
    //verify the user's authentication token
    var authHeader = context.Request.Headers["Authorization"].ToString();
    var client = httpClientFactory.CreateClient();
    //verifyAuth(authHeader);

    //make sure rating is between 1 - 5
    if (request.rating < 1 || request.rating > 5)
        return Results.BadRequest(new { error = "rating must be between 1 and 5" });

    var finish_ride_payload = new
    {
        rideId = request.rideID,
        driverId = 123,
        current_location = new 
        { 
        latitude = 60.123,
        longtitude = -70.123,
        address = "108 University Ave E, Waterloo"
        }
    };
    //update table for end time and driver rating (likely sending rating to the driver module)
    var driverResponse = await client.PostAsJsonAsync("https://localhost:7126/api/DriverManager/DriverComplete", finish_ride_payload);

    //return 202 ok
    return Results.Accepted();
})
.WithName("finishRide")
.WithOpenApi();

app.Run();

public record RideRequest(
    int userID,
    string pickup_address,
    string destination_address,
    string car_type,
    bool pet_friendly
);

public record finishRide(
    int userID,
    int rideID,
    bool rideCompleted,
    int rating
);
public record authResponse(
  int ride_id,
  string status
);
public record navEstimateResponse(
    double distanceKM,
    double fare,
    double durationMinutes,
    string polyline
);
public record driverResponse(
    string driver_name,
    string license_plate,
    string car_model
);
//verify user 
public record UserInfo
{
    public string account_id;
    public string username;
    public string email;
    public string role;
}

//responsible for verifyin user authentication 
public class AuthService
{
    private readonly HttpClient _httpClient;
    //constructor
    public AuthService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Authorization");

    }
    //method to verify user token by calling auth-data module
    public async Task<bool> verifyAuth(string token)
    {
        if (token == null) return false;
        //buil Http request 
        var request = new HttpRequestMessage(HttpMethod.Get, "me");
        //dds token on authentication header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);



        try
        {
            //sends request 
            var response = await _httpClient.SendAsync(request);
            //if return is success code e.g 200
            if (response.IsSuccessStatusCode)
                return true;

            else
                return false;

            //if user has to be verified 
            //var content = await response.Content.ReadAsStringAsync();

            //var user = JsonSerializer.Deserialize<UserInfo>(content);
            //if (user == null) return false;
            //else
            //    return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

}

//May be needed later
//public record driverResponse(
//   int ride_id,
//   int clientId,
//   string timestamp,
//   Location pickup,
//   Location dropOff,
//   routeInformation routeInformation,
//   rideInformation rideInformation
//);

//public record Location(
//    double latitude,
//    double longitude,
//    string address
//);

//public record routeInformation(
//double distanceKM,
//double duration
//);
//public record rideInformation(
//string carType,
//bool petFriendly
//);

