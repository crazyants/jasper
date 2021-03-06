﻿using System;
using Jasper.Internals.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace Jasper.Internals.Codegen
{
    public class SingletonVariableSource : IVariableSource
    {
        private readonly ServiceGraph _graph;

        public SingletonVariableSource(ServiceGraph graph)
        {
            _graph = graph;
        }

        public bool Matches(Type type)
        {
            if (type == typeof(IServiceScopeFactory)) return true;

            var descriptor = _graph.FindDefault(type);
            return descriptor?.Lifetime == ServiceLifetime.Singleton;
        }

        public Variable Create(Type type)
        {
            return new InjectedField(type);
        }
    }
}