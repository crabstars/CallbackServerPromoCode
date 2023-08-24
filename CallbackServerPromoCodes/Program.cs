using CallbackServerPromoCodes;
using CallbackServerPromoCodes.Authentication;
using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Manager;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Serilog;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

var builder = WebApplication.CreateBuilder(args);
var configuration = ConfigurationProvider.GetConfiguration();

var loggingPath = configuration[AppSettings.Serilog] ?? "logs/promo-code.txt";
var serilogLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(loggingPath)
    .MinimumLevel.Information()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(serilogLogger);

builder.Services.AddDbContext<AppDbContext>();
var app = builder.Build();
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.Migrate();

app.MapPost("api/youtube-feed",
    async (AppDbContext context, ILoggerFactory loggerFactory, HttpContext c) =>
    {
        var logger = loggerFactory.CreateLogger("controller");
        using var reader = new StreamReader(c.Request.Body);
        var xmlContent = await reader.ReadToEndAsync();

        c.Request.Headers.TryGetValue(Auth.PubSubHubSig, out var signature);
        // return 2xx to ack receipt even if wrong sig (point 8) http://pubsubhubbub.github.io/PubSubHubbub/pubsubhubbub-core-0.4.html
        if (!Hmac.Verify(logger, xmlContent, signature))
            return Results.Ok("Wrong signature");

        try
        {
            var result = XmlManager.ToYoutubeFeed(xmlContent);
            if (result is null)
            {
                logger.LogError("deserialized result is null, XML-Data: {xmlContent}", xmlContent);
                return Results.BadRequest("Could not deserialize xml");
            }

            var channel = await context.Channels.FirstOrDefaultAsync(c => c.Id == result.Entry.ChannelId);
            if (channel is null)
            {
                logger.LogError("Channel was not added {channelId}", result.Entry.ChannelId);
                return Results.BadRequest("Could not deserialize xml");
            }

            var video = await DbManager.AddVideo(result, channel, context);
            return Results.Ok(video);
        }
        catch (Exception e)
        {
            logger.LogError("Exception: {e}, XML-Data: {xmlContent}", e, xmlContent);
            return Results.BadRequest("Invalid XML data.");
        }
    }).Accepts<HttpRequest>("application/xml");

app.MapGet("api/youtube-feed", (HttpContext c) =>
{
    if (!c.Request.Query.TryGetValue(Auth.HubChallenge, out var hubChallengeValue))
    {
        c.Response.StatusCode = 404;
        c.Response.WriteAsync("missing " + Auth.HubChallenge);
    }

    var hubChallenge = hubChallengeValue.ToString();
    c.Response.WriteAsync(hubChallenge);
});


app.MapPost("api/youtube-feed/creator", async (string name, bool subscribe) =>
{
    // check if already exists
    var httpClient = new HttpClient(); // POol or Factory
    var config = ConfigurationProvider.GetConfiguration();
    var responseBody = "";
    var apiKey = configuration.GetSection("Secrets:YoutubeApiKey").Value ??
                 throw new ArgumentException("Missing YoutubeApiKey in appsetting.json");
    var apiUrl =
        $"https://www.googleapis.com/youtube/v3/search?key={apiKey}&q={name}&type=channel&part=snippet"; // Replace with your API key
    try
    {
        var response = await httpClient.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            // Read the content as a string
            responseBody = await response.Content.ReadAsStringAsync();

            // Now you can work with the response data (e.g., parse JSON)
            Console.WriteLine(responseBody);
        }
        else
        {
            Console.WriteLine($"HTTP Error: {response.StatusCode}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Request Error: {ex.Message}");
    }

    var jsonObject = JObject.Parse(responseBody);
    var items = (JArray)jsonObject["items"];

    if (items.Count > 0)
    {
        // Get the "channelId" from the first item
        var channelId = (string)items[0]["id"]["channelId"];
        Console.WriteLine("Channel ID: " + channelId);
    }
    else
    {
        Console.WriteLine("No items found in the response.");
    }
}).AddEndpointFilter<ApiKeyEndpointFilter>();

// TODO api call to change subscription

app.MapGet("api/pubSubHubSubscription", async (string channelId, HttpContext c) =>
{
    var httpClient = new HttpClient();
    var hmacSecret = configuration.GetSection("Secrets:HmacPubSubHub").Value ??
                     throw new ArgumentException("Missing secret for HmacPubSubHub in appsetting.json");

    var apiUrl =
        $"https://pubsubhubbub.appspot.com/subscription-details?hub.callback=https://promo-codes.duckdns.org/api/youtube-feed&hub.topic=https://www.youtube.com/xml/feeds/videos.xml?channel_id=UCUyeluBRhGPCW4rPe_UvBZQ&hub.secret={hmacSecret}";
    try
    {
        var response = await httpClient.GetAsync(apiUrl);

        if (response.IsSuccessStatusCode)
        {
            // Read the content as a string
            var responseBody = await response.Content.ReadAsStringAsync();

            // Now you can work with the response data (e.g., parse JSON)
            await c.Response.WriteAsync(responseBody);
        }
        else
        {
            c.Response.StatusCode = 404;
            await c.Response.WriteAsync(response.IsSuccessStatusCode.ToString());
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Request Error: {ex.Message}");
    }
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.MapDelete("api/youtube-feed/creator", () => { }).AddEndpointFilter<ApiKeyEndpointFilter>();


app.MapGet("api/videos", (AppDbContext context)
    => Results.Ok(context.Videos.ToList())).AddEndpointFilter<ApiKeyEndpointFilter>();


// TODO calls for get promo codes by company or youtube channel name => return promo code or link and link to yt video
app.Run();