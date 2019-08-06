using Microsoft.Azure.WebJobs.Host.Executors;

namespace Younited.MassTransit.Trigger.Binding
{
    internal interface IMassTransitListenerFactory
    {
        IMassTransitBusListener GetListener<TMessage>(string busName, string queueName, TriggerParameterMode triggerParameterMode, SessionUsage sessionUsage, ITriggeredFunctionExecutor contextExecutor)
            where TMessage : class;
    }
}
