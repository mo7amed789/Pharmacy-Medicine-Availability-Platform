namespace PharmacyAvailability.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking by disallowing framing
            headers["X-Frame-Options"] = "DENY";

            // Modern browser standard for reflected XSS filtering
            headers["X-XSS-Protection"] = "0";

            // Restrict referrer information sent on cross-origin requests
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // API responses must not be embedded or treated as a cross-origin resource by default.
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=(), payment=(), usb=()";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
