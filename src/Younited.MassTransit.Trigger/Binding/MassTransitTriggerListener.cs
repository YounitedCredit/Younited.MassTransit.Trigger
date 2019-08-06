using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Younited.MassTransit.Trigger.Binding
{
    internal class MassTransitTriggerListener<TMessage> : IListener
        where TMessage : class
    {
        private IMassTransitBusListener BusControl { get; }

        public MassTransitTriggerListener(IMassTransitListenerFactory listenerFactory,
            string busName,
            string queueName,
            TriggerParameterMode triggerParameterMode,
            SessionUsage sessionUsage,
            ITriggeredFunctionExecutor contextExecutor)
        {
            BusControl = listenerFactory.GetListener<TMessage>(busName, queueName, triggerParameterMode, sessionUsage, contextExecutor);
        }

        public void Dispose()
        {
            // nothing to dispose
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await BusControl.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await BusControl.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Cancel()
        {
            StopAsync(CancellationToken.None).Wait();
        }
    }
}
