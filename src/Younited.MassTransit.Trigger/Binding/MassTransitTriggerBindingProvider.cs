using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Younited.MassTransit.Trigger.Binding
{
    internal class MassTransitTriggerBindingProvider : ITriggerBindingProvider
    {
        private static readonly Task<ITriggerBinding> NullTriggerBindingTask = Task.FromResult<ITriggerBinding>(null);

        private IServiceProvider ServiceProvider { get; }
        public INameResolver NameResolver { get; }

        public MassTransitTriggerBindingProvider(IServiceProvider serviceProvider, INameResolver nameResolver)
        {
            ServiceProvider = serviceProvider;
            NameResolver = nameResolver;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var parameter = context.Parameter;

            var triggerAttribute = parameter.GetCustomAttribute<MassTransitServiceBusTriggerAttribute>(false);
            if (triggerAttribute is null)
            {
                return NullTriggerBindingTask;
            }

            if (parameter.ParameterType.IsValueType)
            {
                return NullTriggerBindingTask;
            }

            var listenerFactory = new MassTransitListenerFactory(ServiceProvider);

            var (messageType, mode) = GetMessageType(parameter.ParameterType);

            var method = GetType().GetMethod(nameof(BuildBinding), BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(method != null);
            var closedMethod = method.MakeGenericMethod(messageType);
            var sessionUsage = triggerAttribute.UseSession ? SessionUsage.Activated : SessionUsage.None;

            var busName = NameResolver.ResolveWholeString(triggerAttribute.Bus);
            var queueName = NameResolver.ResolveWholeString(triggerAttribute.QueueName);

            var binding = (ITriggerBinding)closedMethod.Invoke(
                null,
                new object[] { listenerFactory, busName, queueName, mode, sessionUsage, parameter }
                );

            return Task.FromResult(binding);
        }

        private static (Type MessageType, TriggerParameterMode Mode) GetMessageType(Type parameterType)
        {
            if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(ConsumeContext<>))
            {
                return (parameterType.GenericTypeArguments.First(), TriggerParameterMode.ConsumeContext);
            }

            return (parameterType, TriggerParameterMode.Message);
        }

        private static MassTransitTriggerBinding<TMessage> BuildBinding<TMessage>(
            IMassTransitListenerFactory listenerFactory, string busName, string queueName, TriggerParameterMode triggerParameterMode, SessionUsage sessionUsage, ParameterInfo parameter)
            where TMessage : class
        {
            return new MassTransitTriggerBinding<TMessage>(listenerFactory, busName, queueName, triggerParameterMode, sessionUsage, parameter);
        }
    }
}
