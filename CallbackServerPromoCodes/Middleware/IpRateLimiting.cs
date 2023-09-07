using System.Net;
using CallbackServerPromoCodes.Constants;

namespace CallbackServerPromoCodes.Middleware;

/// <summary>
///     cached results are not counted
/// </summary>
public class IpRateLimiting
{
    private const int MaxRequest = 1;
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly Timer _resetTimer;
    private Dictionary<string, int> _ipRequestCounter;

    public IpRateLimiting(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _ipRequestCounter = new Dictionary<string, int>();
        _resetTimer = new Timer(ResetCounter, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        _logger = loggerFactory.CreateLogger<IpRateLimiting>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldApplyMiddleware(context))
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            if (ipAddress is null)
            {
                await _next(context);
                return;
            }

            if (_ipRequestCounter.TryGetValue(ipAddress, out var dailyRequests))
            {
                await CheckRequestLimit(context, dailyRequests);
                _ipRequestCounter[ipAddress] += 1;
            }
            else
            {
                _ipRequestCounter[ipAddress] = 1;
            }

            await _next(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task CheckRequestLimit(HttpContext context, int dailyRequests)
    {
        if (dailyRequests > MaxRequest)
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

    private static bool ShouldApplyMiddleware(HttpContext context)
    {
        return context.Request.Path.Value!.Contains(URLPath.Promotions);
    }
}