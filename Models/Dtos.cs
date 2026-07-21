namespace mlm.Models;

/// <summary>Payload for self-registration.</summary>
public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Username,
    string Password,
    Guid? SponsorId,
    string? ReferralCode);

/// <summary>Payload for logging in with username or email.</summary>
public record LoginRequest(string UsernameOrEmail, string Password);

/// <summary>Safe user projection returned to clients (no password hash / security fields).</summary>
public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Username,
    string ReferralCode,
    Guid? SponsorId,
    DateTime CreatedAt);

/// <summary>Public lookup of a referral code (shown on the signup page).</summary>
public record ReferralLookupDto(string SponsorName);

/// <summary>The signed-in user's own referral info.</summary>
public record MyReferralDto(string Code, int DirectCount);

/// <summary>A single member in a referral list or downline (with depth).</summary>
public record ReferralNodeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Username,
    Guid? SponsorId,
    int Level,
    DateTime CreatedAt);

/// <summary>A member's full downline with summary stats.</summary>
public record DownlineDto(int TotalCount, int MaxDepth, IReadOnlyList<ReferralNodeDto> Members);

public static class UserMappingExtensions
{
    public static UserDto ToDto(this AppUser user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.UserName,
            user.ReferralCode, user.SponsorId, user.CreatedAt);
}

/// <summary>Row shape for the recursive downline SQL query.</summary>
public class DownlineRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public Guid? SponsorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Level { get; set; }
}
