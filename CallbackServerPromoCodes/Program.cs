using System.Xml.Serialization;
using CallbackServerPromoCodes;
using CallbackServerPromoCodes.Models;
using CallbackServerPromoCodes.XML.YouTubeFeedSerialization;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// remove default logging providers
builder.Logging.ClearProviders();
// Serilog configuration    
// TODO change to configuration
var loggingPath = "/mnt/logs/promo-code.txt";
#if DEBUG
loggingPath = "logs/promo-code.txt";
#endif
var serilogLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(loggingPath)
    .MinimumLevel.Information()
    .CreateLogger();
// Register Serilog
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
        var reader = new StreamReader(c.Request.Body);
        var serializer = new XmlSerializer(typeof(Feed));
        var xmlContent = await reader.ReadToEndAsync();
        try
        {
            using var stringReader = new StringReader(xmlContent);
            var result = (Feed)serializer.Deserialize(stringReader);
            if (result is null)
            {
                logger.LogError("deserialized result is null, XML-Data: {xmlContent}",
                    xmlContent);
                return Results.BadRequest("Could not deserialize xml");
            }

            if (await context.Videos.AnyAsync(v => v.VideoId == result.Entry.VideoId))
                return Results.Ok("Video already added");
            await context.Videos.AddAsync(new Video(result.Entry.VideoId, result.Entry.ChannelId));
            await context.SaveChangesAsync();
            return Results.Ok(result.Entry.VideoId);
        }
        catch (Exception e)
        {
            logger.LogError("Exception: {e}, XML-Data: {xmlContent}", e, xmlContent);
            return Results.BadRequest("Invalid XML data.");
        }
    }).Accepts<HttpRequest>("application/xml");


app.MapGet("api/youtube-feed", (HttpContext c) =>
{
    if (!c.Request.Query.TryGetValue("hub.challenge", out var hubChallengeValue))
    {
        c.Response.StatusCode = 404;
        c.Response.WriteAsync("missing hub.challenge");
    }

    var hubChallenge = hubChallengeValue.ToString();

    c.Response.WriteAsync(hubChallenge);
});

app.MapGet("api/videos", (AppDbContext context) => Results.Ok(context.Videos.ToList()));

app.Run();