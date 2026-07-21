using Microsoft.AspNetCore.Identity;

namespace mlm.Models;

/// <summary>
/// A member of the MLM network. Extends the ASP.NET Core Identity user with the
/// referral relationship (SponsorId) that forms the network tree.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        // Identity does not generate the key for a Guid PK, so seed it here.
        Id = Guid.NewGuid();
    }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Unique, shareable code others use to sign up under this member.</summary>
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>The user who referred this member. Null for the root member.</summary>
    public Guid? SponsorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
