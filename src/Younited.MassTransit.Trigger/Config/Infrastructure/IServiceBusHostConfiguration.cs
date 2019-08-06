using System;
using MassTransit.Azure.ServiceBus.Core;

namespace Younited.MassTransit.Trigger.Config.Infrastructure
{
    public interface IServiceBusHostConfiguration
    {
        Uri ServiceUri { get; }

        void ConfigureHost(IServiceBusHostConfigurator configurator);
    }
}
