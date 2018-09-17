﻿using System;
using System.Linq;
using DryIoc;
using RoastedMarketplace.Core.Infrastructure;
using RoastedMarketplace.Core.Infrastructure.Utils;

namespace RoastedMarketplace.Infrastructure.DependencyContainer
{
    public class CompositionRoot
    {
        public CompositionRoot(IRegistrator registrar)
        {
            //now the other dependencies by other modules or system
            var dependencies = TypeFinder.ClassesOfType<IDependencyContainer>();
            //create instances for them
            var dependencyInstances = dependencies.Select(dependency => (IDependencyContainer)Activator.CreateInstance(dependency)).ToList();
            //reorder according to priority
            dependencyInstances = dependencyInstances.OrderBy(x => x.Priority).ToList();

            foreach (var di in dependencyInstances)
                //register individual instances in that order
                di.RegisterDependencies(registrar);
        }
    }
}