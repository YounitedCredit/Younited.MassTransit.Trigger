using MassTransit.ConsumeConfigurators;

namespace Younited.MassTransit.Trigger.Config.Infrastructure
{
    public interface IServiceBusTriggerConfiguration<TMessage> where TMessage : class
    {
        void Configure(IHandlerConfigurator<TMessage> configurator);
    }
}
