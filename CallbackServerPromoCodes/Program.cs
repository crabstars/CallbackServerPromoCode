using CallbackServerPromoCodes;
using CallbackServerPromoCodes.Authentication;
using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Manager;
using CallbackServerPromoCodes.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

var builder = WebApplication.CreateBuilder(args);
var configuration = ConfigurationProvider.GetConfiguration();

var loggingPath = configuration[AppSettings.Serilog] ?? "logs/promo-code.txt";
var serilogLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(loggingPath)
    .MinimumLevel.Debug()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(serilogLogger);

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHttpClient();

// Add background worker
builder.Services.AddHostedService<ProcessVideo>();

var app = builder.Build();
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.Migrate();

app.MapPost("api/youtube-feed",
    async (AppDbContext appDbContext, ILoggerFactory loggerFactory, HttpContext httpContext) =>
    {
        var logger = loggerFactory.CreateLogger("post-youtube-feed");
        using var reader = new StreamReader(httpContext.Request.Body);
        var requestBody = await reader.ReadToEndAsync();

        httpContext.Request.Headers.TryGetValue(Auth.PubSubHubSig, out var signature);
        // return 2xx to ack receipt even if wrong sig (point 8) http://pubsubhubbub.github.io/PubSubHubbub/pubsubhubbub-core-0.4.html
        if (!Hmac.Verify(logger, requestBody, signature))
        {
            logger.LogError("Got request with wrong signature. Body: {body}", requestBody);
            return Results.Ok("Wrong signature");
        }

        try
        {
            var xmlContent = XmlManager.ToYoutubeFeed(requestBody);
            if (xmlContent is null)
            {
                logger.LogError("deserialized result is null, XML-Data: {xmlContent}", requestBody);
                return Results.Ok("Could not deserialize xml");
            }

            var channel = await appDbContext.Channels.FirstOrDefaultAsync(c => c.Id == xmlContent.Entry.ChannelId);
            if (channel is null || !channel.Subscribed || !channel.Activated)
            {
                logger.LogError("Channel was not added {channelId} or is not active", xmlContent.Entry.ChannelId);
                return Results.Ok("Missing Channel");
            }

            var video = await DbManager.AddVideo(xmlContent, channel, appDbContext);
            logger.LogDebug("VideoId: {id}, Link: {link}", video.Id, video.Link);
        }
        catch (Exception e)
        {
            logger.LogError("Exception: {e}, XML-Data: {xmlContent}", e, requestBody);
        }

        return Results.Ok();
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


app.MapPost("api/youtube-feed/creator", async (AppDbContext context, string name,
    IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("post-youtube-feed-creator");
    var httpClient = httpClientFactory.CreateClient();

    var apiKey = configuration.GetSection("Secrets:YoutubeApiKey").Value ??
                 throw new ArgumentException("Missing YoutubeApiKey in appsettings.json");

    var responseBody =
        await YoutubeRequestManager.GetResponseForYoutubeChannelsCall(apiKey, name, httpClient, logger,
            cancellationToken);
    if (responseBody is null)
        return Results.BadRequest("Error occured while calling youtube api, see logs");

    var channel = ParseManager.GetChannel(responseBody, logger);
    if (channel is null)
        return Results.BadRequest("No channel found");

    logger.LogDebug("ChannelId: {id}, Name: {link}", channel.Id, channel.Name);
    if (await DbManager.AddChannel(context, channel, cancellationToken) is null)
        return Results.Ok("Channel already inserted");

    return Results.Ok("Channel inserted");
}).AddEndpointFilter<ApiKeyEndpointFilter>();

// TODO api call to change subscription

app.MapGet("api/pubSubHubSubscription", async (string channelId, IHttpClientFactory httpClientFactory, HttpContext c) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var hmacSecret = configuration.GetSection("Secrets:HmacPubSubHub").Value ??
                     throw new ArgumentException("Missing secret for HmacPubSubHub in appsettings.json");

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