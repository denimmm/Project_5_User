using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Writers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

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
bool verifyAuth(String? auth_header)
{
    //make sure string is not empty
    if (string.IsNullOrEmpty(auth_header))
        return false;


    //send to authentication module for verification


    return true;

}



//Maksym
//allows the user to request a ride and receive an offer. offer must be confirmed
// /api/create_new_trip
////input: POST { "userID": "u12345", "pickup_address": "Conestoga College, Waterloo, ON", "destination_address": "Conestoga Mall, Waterloo, ON", "car_type" : "XL", "pet_friendly" : "true"}
////output: { "rideID" : "01242", "distanceKm" : "14.58", "fare" : "29.04", "durationMinutes" : "1.86", "driver_name" : "Matthew", "license_plate" : "KJVM 719", "car_model" : "Biege Chevy Malibu"}
app.MapPost("/create_new_trip", async (RideRequest request, IHttpClientFactory httpClientFactory, HttpContext context) =>
{
    //authenticate
    //verify the user's authentication token
    var authHeader = context.Request.Headers["Authorization"].ToString();

    //verifyAuth(authHeader);

    //make http client to access navigation authentication and driver endpoints
    var client = httpClientFactory.CreateClient();

    //get estimate from navigation module
    var navInput = new
    {
        pickupAddress = request.pickup_address,
        destinationAddress = request.destination_address
    };

    //call navigation module /api/estimate
    var navEstimateResponse = await client.PostAsJsonAsync("https://portainer.gooberapp.org:2342/api/estimate", navInput);

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
    var authResponse = await client.PostAsJsonAsync("https://portainer.gooberapp.org:3456/api/authentication/create_new_trip", authRequestJson);

    //Populate auth content object
    var authContent = await authResponse.Content.ReadFromJsonAsync<authResponse>();
    
    var DriverRequestJson = new
    {
        ride_id = authContent.ride_id
    };

    //call driver module to get assigned driver
    var driverResponse = await client.PostAsJsonAsync("https://portainer.gooberapp.org:4567/api/driver/assign_driver", DriverRequestJson);

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

app.MapPost("/confirm_trip", (HttpContext context, int userID, bool trip_confirmed) =>
{
    //authenticate
    var authHeader = context.Request.Headers["Authorization"].ToString();
    //verifyAuth(authHeader);


    //activate payment with payment module
    if (trip_confirmed)
    {
        //confirm payment in ride table entry
        //start the ride

        //payment failed
        if (false)
        {
            return Results.Problem(
                "Payment could not be processed.",
                statusCode: 402
            );

        }
    }
    else
    {
        //cancel the ride
        return Results.Ok();
    }





    return Results.Accepted();

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
    int port = 2342; //placeholder
    string driverID = "001"; //placeholder. will need to rettrieve this from the auth-data team or give it to the user in confirm_ride for the user to send back to us here.
    string navurl = $"https://portainer.gooberapp.org:{port}/lastLocation?driverID={driverID}";

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
app.MapPost("/finish_ride", (finishRide request, HttpContext context) =>
{
    //authenticate
    //verify the user's authentication token
    var authHeader = context.Request.Headers["Authorization"].ToString();

    //verifyAuth(authHeader);

    //make sure rating is between 1 - 5
    if (request.rating < 1 || request.rating > 5)
        return Results.BadRequest(new {error = "rating must be between 1 and 5"});


    //update table for end time and driver rating (likely sending rating to the driver module)


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

