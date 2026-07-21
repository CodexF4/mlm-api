using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mlm.Data;
using mlm.Models;

namespace mlm.Endpoints;

public static class ReferralEndpoints
{
    public static IEndpointRouteBuilder MapReferralEndpoints(this IEndpointRouteBuilder app)
    {
        // Public: resolve a referral code to a sponsor name (shown on the signup page).
        app.MapGet("/referral/{code}", async (string code, UserManager<AppUser> users) =>
        {
            var sponsor = await users.Users.FirstOrDefaultAsync(u => u.ReferralCode == code);
            return sponsor is null
                ? Results.NotFound()
                : Results.Ok(new ReferralLookupDto($"{sponsor.FirstName} {sponsor.LastName}"));
        })
        .WithName("LookupReferral")
        .WithTags("Referrals");

        // The signed-in user's own referral code + direct-referral count.
        app.MapGet("/me/referral", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null)
            {
                return Results.NotFound();
            }

            var directCount = await users.Users.CountAsync(u => u.SponsorId == me.Id);
            return Results.Ok(new MyReferralDto(me.ReferralCode, directCount));
        })
        .RequireAuthorization()
        .WithName("MyReferral")
        .WithTags("Referrals");

        // The signed-in user's direct referrals.
        app.MapGet("/me/referrals", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null)
            {
                return Results.NotFound();
            }

            var referrals = await users.Users
                .Where(u => u.SponsorId == me.Id)
                .OrderBy(u => u.CreatedAt)
                .Select(u => new ReferralNodeDto(
                    u.Id, u.FirstName, u.LastName, u.UserName, u.SponsorId, 1, u.CreatedAt))
                .ToListAsync();

            return Results.Ok(referrals);
        })
        .RequireAuthorization()
        .WithName("MyReferrals")
        .WithTags("Referrals");

        // The signed-in user's full downline (all levels) via a recursive CTE.
        app.MapGet("/me/downline", async (ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null)
            {
                return Results.NotFound();
            }

            const string sql = """
                WITH RECURSIVE downline AS (
                    SELECT "Id", "FirstName", "LastName", "UserName", "SponsorId", "CreatedAt", 1 AS "Level"
                    FROM "AspNetUsers"
                    WHERE "SponsorId" = {0}
                    UNION ALL
                    SELECT u."Id", u."FirstName", u."LastName", u."UserName", u."SponsorId", u."CreatedAt", d."Level" + 1
                    FROM "AspNetUsers" u
                    INNER JOIN downline d ON u."SponsorId" = d."Id"
                )
                SELECT "Id", "FirstName", "LastName", "UserName", "SponsorId", "CreatedAt", "Level"
                FROM downline
                ORDER BY "Level", "CreatedAt"
                """;

            var rows = await db.Database.SqlQueryRaw<DownlineRow>(sql, me.Id).ToListAsync();

            var members = rows
                .Select(r => new ReferralNodeDto(
                    r.Id, r.FirstName, r.LastName, r.UserName, r.SponsorId, r.Level, r.CreatedAt))
                .ToList();

            var maxDepth = members.Count == 0 ? 0 : members.Max(m => m.Level);
            return Results.Ok(new DownlineDto(members.Count, maxDepth, members));
        })
        .RequireAuthorization()
        .WithName("MyDownline")
        .WithTags("Referrals");

        return app;
    }
}
