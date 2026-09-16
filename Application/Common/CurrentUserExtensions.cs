using InvestManagement.Application.Common.Interfaces;

namespace InvestManagement.Application.Common;

public static class CurrentUserExtensions
{
    public static Guid GetRequiredUserId(this ICurrentUser currentUser)
    {
        return currentUser.UserId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("An authenticated user is required.");
    }
}
