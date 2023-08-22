using CallbackServerPromoCodes.Constants;

namespace CallbackServerPromoCodes.Authentication;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;

    public ApiKeyEndpointFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(Auth.ApiKeyHeader, out var requestApiKey))
            return TypedResults.Unauthorized();

        var apiKey = _configuration.GetValue<string>(AppSettings.CallbackApiKey);
        if (apiKey == null || !apiKey.Equals(requestApiKey)) return TypedResults.Unauthorized();

        return await next(context);
    }
}