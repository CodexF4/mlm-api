using Microsoft.AspNetCore.Http;

namespace mlm.Security;

/// <summary>The httpOnly cookie that carries the JWT access token.</summary>
public static class AuthCookie
{
    public const string Name = "access_token";

    public static void Append(HttpResponse response, string token, DateTime expiresAt)
    {
        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = response.HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt
        });
    }
}
