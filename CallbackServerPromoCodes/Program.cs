using CallbackServerPromoCodes;
using CallbackServerPromoCodes.Authentication;
using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.Manager;
using Microsoft.EntityFrameworkCore;
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

            var video = await DbManager.AddVideo(result, context);
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


app.MapPost("api/youtube-feed/creator", () =>
{
    // convert usernmae to id https://www.googleapis.com/youtube/v3/channels?part=snippet&forUsername={USERNAME}&key={YOUR_API_KEY}
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.MapDelete("api/youtube-feed/creator", () => { }).AddEndpointFilter<ApiKeyEndpointFilter>();


app.MapGet("api/videos", (AppDbContext context)
    => Results.Ok(context.Videos.ToList())).AddEndpointFilter<ApiKeyEndpointFilter>();

app.Run();