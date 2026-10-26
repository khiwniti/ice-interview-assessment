using Microsoft.AspNetCore.Identity;

namespace BusinessAnalytics.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? BusinessUserId { get; set; }
    public string? DisplayName { get; set; }
}