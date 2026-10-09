using PocketBot.Core.Access;

namespace PocketBot.Core.Tests.Access;

public class AccessPolicyTests
{
    [Fact]
    public void Whitelisted_user_is_allowed()
    {
        new AccessPolicy([111, 222]).IsAllowed(222).ShouldBeTrue();
    }

    [Fact]
    public void User_outside_the_whitelist_is_denied()
    {
        new AccessPolicy([111]).IsAllowed(999).ShouldBeFalse();
    }

    [Fact]
    public void Empty_whitelist_denies_everyone()
    {
        new AccessPolicy([]).IsAllowed(111).ShouldBeFalse();
    }
}
