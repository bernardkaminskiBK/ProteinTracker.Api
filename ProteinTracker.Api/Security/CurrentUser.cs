using System.Security.Claims;

namespace ProteinTracker.Api.Security;

public sealed class CurrentUser
{
    private readonly IHttpContextAccessor? httpContextAccessor;
    private readonly int? testUserId;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser(int userId)
    {
        testUserId = userId;
    }

    public int Id
    {
        get
        {
            if (testUserId.HasValue)
            {
                return testUserId.Value;
            }

            var value = httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId)
                ? userId
                : throw new InvalidOperationException("An authenticated user is required.");
        }
    }
}
