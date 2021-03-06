﻿using System;
using Jasper.Bus.ErrorHandling;
using Jasper.Bus.Model;

namespace Jasper.Bus.Configuration
{
    /// <summary>
    /// Applies an error policy that a message should be retried
    /// whenever processing encounters the designated exception type
    /// </summary>
    public class RetryOnAttribute : ModifyHandlerChainAttribute
    {
        private readonly Type _exceptionType;

        public RetryOnAttribute(Type exceptionType)
        {
            _exceptionType = exceptionType;
        }

        public override void Modify(HandlerChain chain)
        {
            chain.OnException(_exceptionType).Retry();
        }
    }
}
