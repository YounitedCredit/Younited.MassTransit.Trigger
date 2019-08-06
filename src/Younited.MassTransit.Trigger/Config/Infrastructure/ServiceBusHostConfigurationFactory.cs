using System;
using System.Collections.Generic;
using System.Linq;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ConsumeConfigurators;

namespace Younited.MassTransit.Trigger.Config.Infrastructure
{
    public class ServiceBusHostConfigurationFactory : IServiceBusHostConfigurationFactory
    {
        private Dictionary<string, IServiceBusHostConfiguration> HostConfigurations { get; } = new Dictionary<string, IServiceBusHostConfiguration>();
        private Dictionary<Type, Dictionary<string, object>> BusTriggerConfigurations { get; } = new Dictionary<Type, Dictionary<string, object>>();
        private Dictionary<Type, object> TriggerConfigurations { get; } = new Dictionary<Type, object>();

        public IServiceBusHostConfiguration GetHostConfiguration(string busName)
        {
            HostConfigurations.TryGetValue(busName, out var configuration);
            return configuration;
        }

        public void RegisterHostConfiguration(string busName, Uri serviceUri, Action<IServiceBusHostConfigurator> configure)
        {
            HostConfigurations.Add(busName, new HostConfiguration(serviceUri, configure));
        }

        public void RegisterTriggerConfiguration<TMessage>(Action<IHandlerConfigurator<TMessage>> configure) where TMessage : class
        {
            TriggerConfigurations.Add(typeof(TMessage), new TriggerConfiguration<TMessage>(configure));
        }

        public void RegisterTriggerConfiguration<TMessage>(string busName, Action<IHandlerConfigurator<TMessage>> configure) where TMessage : class
        {
            if (!BusTriggerConfigurations.TryGetValue(typeof(TMessage), out var busTriggerConfigurations))
            {
                busTriggerConfigurations = new Dictionary<string, object>();
                BusTriggerConfigurations.Add(typeof(TMessage), busTriggerConfigurations);
            }
            busTriggerConfigurations.Add(busName, new TriggerConfiguration<TMessage>(configure));
        }

        public IServiceBusTriggerConfiguration<TMessage> GetTriggerConfiguration<TMessage>(string busName)
            where TMessage : class
        {
            var configurations = new List<IServiceBusTriggerConfiguration<TMessage>>();
            if (TriggerConfigurations.TryGetValue(typeof(TMessage), out var triggerConfiguration))
            {
                configurations.Add((IServiceBusTriggerConfiguration<TMessage>)triggerConfiguration);
            }

            if (BusTriggerConfigurations.TryGetValue(typeof(TMessage), out var busTriggerConfigurations))
            {
                if (busTriggerConfigurations.TryGetValue(busName, out var busTriggerConfiguration))
                {
                    configurations.Add((IServiceBusTriggerConfiguration<TMessage>)busTriggerConfiguration);
                }
            }

            return new CompositeTriggerConfiguration<TMessage>(configurations);
        }

        private class HostConfiguration : IServiceBusHostConfiguration
        {
            private readonly Action<IServiceBusHostConfigurator> _configure;

            public Uri ServiceUri { get; }

            public HostConfiguration(Uri serviceUri, Action<IServiceBusHostConfigurator> configure)
            {
                ServiceUri = serviceUri;
                _configure = configure;
            }

            public void ConfigureHost(IServiceBusHostConfigurator configurator)
            {
                _configure(configurator);
            }
        }

        private class TriggerConfiguration<TMessage> : IServiceBusTriggerConfiguration<TMessage> where TMessage : class
        {
            private readonly Action<IHandlerConfigurator<TMessage>> _configure;

            public TriggerConfiguration(Action<IHandlerConfigurator<TMessage>> configure)
            {
                _configure = configure;
            }

            public void Configure(IHandlerConfigurator<TMessage> configurator)
            {
                _configure(configurator);
            }
        }

        private class CompositeTriggerConfiguration<TMessage> : IServiceBusTriggerConfiguration<TMessage> where TMessage : class
        {
            private IServiceBusTriggerConfiguration<TMessage>[] Configurations { get; }

            public CompositeTriggerConfiguration(IEnumerable<IServiceBusTriggerConfiguration<TMessage>> configurations)
            {
                Configurations = configurations.ToArray();
            }

            public void Configure(IHandlerConfigurator<TMessage> configurator)
            {
                foreach (var configuration in Configurations)
                {
                    configuration.Configure(configurator);
                }
            }
        }
    }
}
