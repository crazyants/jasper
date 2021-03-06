﻿using System;
using System.Threading.Tasks;
using Jasper.Bus.Runtime.Routing;

namespace Jasper.Bus.Runtime.Subscriptions
{
    public static class RuntimeSubscriptionExtensions
    {
        public static Task PersistSubscriptions(this JasperRuntime runtime)
        {
            var repository = runtime.Get<ISubscriptionsRepository>();
            return repository.PersistCapabilities(runtime.Capabilities);
        }

        public static void ResetSubscriptions(this JasperRuntime runtime)
        {
            runtime.Get<IMessageRouter>().ClearAll();
        }

    }
}
