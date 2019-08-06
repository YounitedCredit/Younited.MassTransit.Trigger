namespace Younited.MassTransit.Trigger.Config.Infrastructure
{
    public interface IServiceBusHostConfigurationFactory
    {
        IServiceBusHostConfiguration GetHostConfiguration(string busName);

        IServiceBusTriggerConfiguration<TMessage> GetTriggerConfiguration<TMessage>(string busName) where TMessage : class;
    }
}
