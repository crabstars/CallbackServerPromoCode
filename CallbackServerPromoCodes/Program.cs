using CallbackServerPromoCodes;
using CallbackServerPromoCodes.Authentication;
using CallbackServerPromoCodes.Constants;
using CallbackServerPromoCodes.DTOs;
using CallbackServerPromoCodes.Enums;
using CallbackServerPromoCodes.Manager;
using CallbackServerPromoCodes.Middleware;
using CallbackServerPromoCodes.Worker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ConfigurationProvider = CallbackServerPromoCodes.Provider.ConfigurationProvider;

var builder = WebApplication.CreateBuilder(args);
var configuration = ConfigurationProvider.GetConfiguration();

var loggingPath = configuration[AppSettings.Serilog] ?? "logs/promo-code.txt";
builder.Logging.ClearProviders();
var serilogLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(loggingPath)
    .MinimumLevel.Debug()
    .CreateLogger();
builder.Logging.AddSerilog(serilogLogger);


builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHttpClient();
builder.Services.AddOutputCache();
builder.Services.AddCors();

// Add background worker
builder.Services.AddHostedService<ProcessVideo>();
builder.Services.AddHostedService<SubscribeViaPubSubHub>();

var app = builder.Build();
app.UseCors(b => b
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);
app.UseOutputCache();

// add middleware
app.UseWhen(context => context.Request.Path.StartsWithSegments(URLPath.Promotions),
    appBuilder => { appBuilder.UseMiddleware<IpRateLimiting>(); });

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.Migrate();

app.MapPost(URLPath.Callback,
    async ([FromServices] AppDbContext appDbContext, [FromServices] ILoggerFactory loggerFactory,
        HttpContext httpContext) =>
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

app.MapGet(URLPath.Callback, (HttpContext c) =>
{
    if (!c.Request.Query.TryGetValue(Auth.HubVerifyToken, out var hubVerifyToken))
    {
        c.Response.StatusCode = 404;
        c.Response.WriteAsync("missing " + Auth.HubVerifyToken);
        return;
    }

    if (hubVerifyToken != configuration.GetSection(AppSettings.VerifyToken).Value)
    {
        c.Response.StatusCode = 404;
        c.Response.WriteAsync("wrong" + Auth.HubVerifyToken);
        return;
    }

    if (!c.Request.Query.TryGetValue(Auth.HubChallenge, out var hubChallengeValue))
    {
        c.Response.StatusCode = 404;
        c.Response.WriteAsync("missing " + Auth.HubChallenge);
        return;
    }

    var hubChallenge = hubChallengeValue.ToString();

    // Results.Ok would return an apllication/json which doesnt work for the pubsubhub
    c.Response.WriteAsync(hubChallenge);
});

// page should start at 1
app.MapGet(URLPath.Promotions, async ([FromServices] AppDbContext context, [FromQuery] string productName,
    [FromQuery] int page, [FromQuery] int count) =>
{
    if (string.IsNullOrWhiteSpace(productName))
        return new List<SearchPromotionDto>();
    var promotions = context.Promotions.Include(p => p.Video).AsQueryable();
    return await promotions.Where(p => EF.Functions.Like(p.Product, productName + "%"))
        .OrderBy(p => p.Added)
        .Skip((page - 1) * count).Take(count).Select(p => new SearchPromotionDto(p.Product, p.Code,
            p.Link, p.Video.Link, p.Video.Channel.Name)).ToListAsync();
}).CacheOutput(x => x.SetVaryByQuery("productName", "page", "count"));

app.MapPost("api/youtube-feed/creator", async ([FromServices] AppDbContext context,
    [FromServices] IHttpClientFactory httpClientFactory, [FromServices] ILoggerFactory loggerFactory,
    [FromQuery] string name, CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("api/youtube-feed/creator");
    var httpClient = httpClientFactory.CreateClient();

    var responseBody =
        await YoutubeRequestManager.GetResponseForYoutubeChannelsCall(name, httpClient, logger,
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

app.MapGet("api/youtube-feed/creator", async ([FromServices] AppDbContext context,
    [FromQuery] string name, [FromQuery] int page, [FromQuery] int count, CancellationToken cancellationToken) =>
{
    return await context.Channels.Where(c => EF.Functions.Like(c.Name, name + "%"))
        .OrderBy(c => c.Name)
        .Skip((page - 1) * count).Take(count).Select(c => new ChannelDto(c.Id, c.Name, c.Subscribed, c.Activated))
        .ToListAsync(cancellationToken);
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.MapGet("api/pubSubHubSubscription", async (HttpContext context, [FromServices] IHttpClientFactory httpClientFactory,
    [FromQuery] string channelId) =>
{
    var httpClient = httpClientFactory.CreateClient();
    await PubSubHubbubRequestManager.GetSubscriptionDetails(httpClient, context, channelId);
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.MapPost("api/pubSubHubSubscription", async ([FromServices] IHttpClientFactory httpClientFactory,
    [FromServices] ILoggerFactory loggerFactory, [FromQuery] string channelId, [FromQuery] HubMode hubMode) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var logger = loggerFactory.CreateLogger("post api/pubSubHubSubscription");
    if (await PubSubHubbubRequestManager.ChangeSubscription(httpClient, logger, hubMode, channelId))
        return Results.Ok();
    return Results.BadRequest();
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.MapDelete("api/youtube-feed/channel", async ([FromServices] AppDbContext context, [FromQuery] string channelId,
        CancellationToken cancellationToken) =>
    await DbManager.DeleteChannel(context, channelId, cancellationToken) is null
        ? Results.BadRequest("no channel found")
        : Results.Ok("channel deleted")).AddEndpointFilter<ApiKeyEndpointFilter>();

app.Run();