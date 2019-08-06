using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ConsumeConfigurators;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.DependencyInjection;
using Younited.MassTransit.Trigger.Config.Infrastructure;

namespace Younited.MassTransit.Trigger.Binding
{
    internal class MassTransitListenerFactory : IMassTransitListenerFactory
    {
        private static readonly IDictionary<string, Listener> Listeners = new Dictionary<string, Listener>();
        private IServiceProvider ServiceProvider { get; }

        public MassTransitListenerFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IMassTransitBusListener GetListener<TMessage>(string busName, string queueName, TriggerParameterMode triggerParameterMode, SessionUsage sessionUsage, ITriggeredFunctionExecutor contextExecutor)
            where TMessage : class
        {
            var configurationFactory = ServiceProvider.GetService<IServiceBusHostConfigurationFactory>();
            if (configurationFactory == null)
            {
                throw new InvalidOperationException("Unable to get the host configuration factory");
            }

            if (!Listeners.TryGetValue(busName, out var listener))
            {
                var hostConfiguration = configurationFactory.GetHostConfiguration(busName);
                if (hostConfiguration == null)
                {
                    throw new InvalidOperationException($"Unable to get the host configuration for {busName}");
                }
                listener = new Listener(hostConfiguration);
                Listeners.Add(busName, listener);
            }
            var triggerConfiguration = configurationFactory.GetTriggerConfiguration<TMessage>(busName)
                                       ?? new DefaultTriggerConfiguration<TMessage>();
            listener.RegisterQueueConfiguration(queueName, (cfg, host) =>
            {
                ConfigureSessionUsage(cfg, sessionUsage);
                cfg.Handler<TMessage>(
                    context => HandleMessageAsync(context, triggerParameterMode, contextExecutor),
                    triggerConfiguration.Configure);
            });
            return listener;
        }

        private void ConfigureSessionUsage(IServiceBusEndpointConfigurator configurator, SessionUsage sessionUsage)
        {
            switch (sessionUsage)
            {
                case SessionUsage.None:
                    break;
                case SessionUsage.Activated:
                    configurator.RequiresSession = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sessionUsage), sessionUsage, null);
            }
        }

        private static async Task HandleMessageAsync<TMessage>(
            ConsumeContext<TMessage> context, TriggerParameterMode triggerParameterMode, ITriggeredFunctionExecutor contextExecutor)
            where TMessage : class
        {
            var input = new TriggeredFunctionData
            {
                TriggerValue = ConvertToParameterType(context, triggerParameterMode),
                TriggerDetails = new Dictionary<string, string>()
            };

            var result = await contextExecutor
                .TryExecuteAsync(input, context.CancellationToken)
                .ConfigureAwait(false);
            if (!result.Succeeded)
            {
                throw result.Exception;
            }
        }

        private static object ConvertToParameterType<TMessage>(ConsumeContext<TMessage> context, TriggerParameterMode triggerParameterMode) where TMessage : class
        {
            switch (triggerParameterMode)
            {
                case TriggerParameterMode.Message:
                    return context.Message;
                case TriggerParameterMode.ConsumeContext:
                    return context;
                default:
                    throw new ArgumentOutOfRangeException(nameof(triggerParameterMode), triggerParameterMode, null);
            }
        }

        private sealed class DefaultTriggerConfiguration<TMessage> : IServiceBusTriggerConfiguration<TMessage> where TMessage : class
        {
            public void Configure(IHandlerConfigurator<TMessage> configurator)
            {
                // do nothing
            }
        }

        private class Listener : IMassTransitBusListener
        {
            private IServiceBusHostConfiguration HostConfiguration { get; }
            private Dictionary<string, List<Action<IServiceBusReceiveEndpointConfigurator, IServiceBusHost>>> QueueConfigurations { get; } = new Dictionary<string, List<Action<IServiceBusReceiveEndpointConfigurator, IServiceBusHost>>>();
            private IBusControl BusControl { get; set; }
            private bool IsBusControlCreated { get; set; }

            public Listener(IServiceBusHostConfiguration hostConfiguration)
            {
                HostConfiguration = hostConfiguration;
            }

            public void RegisterQueueConfiguration(string queueName, Action<IServiceBusReceiveEndpointConfigurator, IServiceBusHost> action)
            {
                if (!QueueConfigurations.TryGetValue(queueName, out var actions))
                {
                    actions = new List<Action<IServiceBusReceiveEndpointConfigurator, IServiceBusHost>>();
                    QueueConfigurations[queueName] = actions;
                }
                actions.Add(action);
            }

            public async Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                var startTask = Task.CompletedTask;
                lock (this)
                {
                    if (!IsBusControlCreated)
                    {
                        BusControl = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                        {
                            var host = cfg.Host(HostConfiguration.ServiceUri, HostConfiguration.ConfigureHost);
                            foreach (var queue in QueueConfigurations)
                            {
                                cfg.ReceiveEndpoint(
                                    host,
                                    queue.Key, c =>
                                    {
                                        foreach (var action in queue.Value)
                                        {
                                            action(c, host);
                                        }
                                    });
                            }
                        });
                        IsBusControlCreated = true;
                        startTask = BusControl.StartAsync(cancellationToken);
                    }
                }

                await startTask;
            }

            public async Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                lock (this)
                {
                    if (!IsBusControlCreated)
                    {
                        return;
                    }
                }
                await BusControl.StopAsync(cancellationToken);
            }
        }
    }
}
