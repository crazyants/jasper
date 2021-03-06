﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jasper.Internals.Codegen;
using Jasper.Internals.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Jasper.Internals.IoC
{
    public class BuildStepPlanner
    {
        public Type ConcreteType { get; }
        private readonly ServiceGraph _graph;
        private readonly IMethodVariables _method;
        private readonly IList<BuildStep> _visited = new List<BuildStep>();
        private readonly Stack<BuildStep> _chain = new Stack<BuildStep>();

        public BuildStepPlanner(Type serviceType, Type concreteType, ServiceGraph graph, IMethodVariables method)
        {
            if (!concreteType.IsConcrete()) throw new ArgumentOutOfRangeException(nameof(concreteType), "Must be a concrete type");

            ConcreteType = concreteType;
            _graph = graph;
            _method = method;


            var ctor = graph.ChooseConstructor(concreteType);
            if (ctor == null)
            {
                CanBeReduced = false;
            }
            else
            {
                Top = new ConstructorBuildStep(serviceType, concreteType, ServiceLifetime.Scoped, ctor);
                Visit(Top);
            }
        }

        public ConstructorBuildStep Top { get; private set; }

        public bool CanBeReduced { get; private set; } = true;

        public void Visit(BuildStep step)
        {
            if (_chain.Contains(step))
            {
                throw new InvalidOperationException("Bi-directional dependencies detected:" + Environment.NewLine + _chain.Select(x => x.ToString()).Join(Environment.NewLine));
            }

            if (_visited.Contains(step))
            {
                return;
            }

            _chain.Push(step);

            foreach (var dep in step.ReadDependencies(this))
            {
                if (dep == null)
                {
                    CanBeReduced = false;
                    return;
                }


                Visit(dep);
            }

            _chain.Pop();
        }

        public BuildStep FindStep(Type type)
        {
            try
            {
                var candidate = _method.AllKnownBuildSteps.FirstOrDefault(x => x.ServiceType == type && x.CanBeReused);
                if (candidate != null) return candidate;

                var step = findStep(type);

                if (step != null)
                {
                    _method.AllKnownBuildSteps.Add(step);
                }

                return step;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Could not determine a BuildStep for '{type.FullName}'", e);
            }
        }

        public BuildStep FindStep(ServiceDescriptor descriptor)
        {
            var candidate = _method.AllKnownBuildSteps.OfType<IServiceDescriptorBuildStep>()
                .FirstOrDefault(x => x.ServiceDescriptor == descriptor && x.CanBeReused);
            if (candidate != null) return candidate as BuildStep;

            var step = findStep(descriptor);
            if (step != null)
            {
                _method.AllKnownBuildSteps.Add(step);
            }

            return step;
        }

        private BuildStep findStep(Type type)
        {
            // INSTEAD, let's pull all variable sources
            // If not a ServiceVariable, use the KnownVariableBuildStep, otherwise use the
            // parent build step and do NOT visit its dependencies
            var variable = _method.TryFindVariable(type, VariableSource.NotServices);

            if (variable != null) return new KnownVariableBuildStep(variable);

            var @default = _graph.FindDefault(type);

            if (@default == null)
            {
                if (EnumerableStep.IsEnumerable(type))
                {
                    return tryFillEnumerableOfAllKnown(type);
                }

                return null;
            }

            return findStep(@default);
        }

        private BuildStep findStep(ServiceDescriptor descriptor)
        {
            if (descriptor?.ImplementationType != null)
            {
                var ctor = _graph.ChooseConstructor(descriptor.ImplementationType);
                if (ctor != null)
                {
                    return new ConstructorBuildStep(descriptor, ctor);
                }
            }

            return null;
        }

        private BuildStep tryFillEnumerableOfAllKnown(Type serviceType)
        {
            var elementType = EnumerableStep.DetermineElementType(serviceType);
            var all = _graph.FindAll(elementType);

            if (!all.All(x => _graph.CanResolve(x)))
            {
                return null;
            }

            var childSteps = all.Select(FindStep).ToArray();
            return new EnumerableStep(serviceType, childSteps);
        }
    }
}
