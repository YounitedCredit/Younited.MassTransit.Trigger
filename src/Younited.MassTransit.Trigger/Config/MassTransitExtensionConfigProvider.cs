using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Younited.MassTransit.Trigger.Binding;

namespace Younited.MassTransit.Trigger.Config
{
    [Extension("MassTransit")]
    internal class MassTransitExtensionConfigProvider : IExtensionConfigProvider
    {
        public IServiceProvider ServiceProvider { get; }
        public INameResolver NameResolver { get; }

        public MassTransitExtensionConfigProvider(IServiceProvider serviceProvider, INameResolver nameResolver)
        {
            ServiceProvider = serviceProvider;
            NameResolver = nameResolver;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var bindingProvider = new MassTransitTriggerBindingProvider(ServiceProvider, NameResolver);
            context.AddBindingRule<MassTransitServiceBusTriggerAttribute>().BindToTrigger(bindingProvider);
        }
    }
}
