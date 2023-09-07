using System.Net;

namespace CallbackServerPromoCodes.Middleware;

/// <summary>
///     cached results are not counted
/// </summary>
public class IpRateLimiting
{
    private const int MaxRequest = 100;
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly Timer _resetTimer;
    private Dictionary<string, int> _ipRequestCounter;

    public IpRateLimiting(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _ipRequestCounter = new Dictionary<string, int>();
        _resetTimer = new Timer(ResetCounter, null, TimeSpan.Zero, Period);
        _logger = loggerFactory.CreateLogger<IpRateLimiting>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (ipAddress is null)
        {
            await _next(context);
            return;
        }

        if (_ipRequestCounter.TryGetValue(ipAddress, out var ipRequestCount))
        {
            await CheckRequestLimit(context, ipRequestCount);
            _ipRequestCounter[ipAddress] += 1;
        }
        else
        {
            _ipRequestCounter[ipAddress] = 1;
        }

        await _next(context);
    }

    private async Task CheckRequestLimit(HttpContext context, int dailyRequests)
    {
        if (dailyRequests >= MaxRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Request limit currently reached, try again in an hour.");
            _logger.LogInformation("User reached limit");
        }
    }

    private void ResetCounter(object? state)
    {
        _ipRequestCounter = new Dictionary<string, int>();
    }
}