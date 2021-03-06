﻿using Jasper.Bus.Runtime;
using Jasper.Bus.Runtime.Subscriptions;
using Jasper.Testing.Util;
using Jasper.Util;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Bus.Runtime.Subscriptions
{
    public class SubscriptionTester
    {
        [Fact]
        public void new_subscription_uses_the_type_alias()
        {
            new Subscription(typeof(MySpecialMessage), "tcp://localhost:2222/queue".ToUri())
                .MessageType.ShouldBe(typeof(MySpecialMessage).FullName);

            new Subscription(typeof(AliasedMessage), "tcp://localhost:2222/queue".ToUri())
                .MessageType.ShouldBe("Alias.1");
        }
    }

    [MessageAlias("Alias.1")]
    public class AliasedMessage
    {

    }
}
