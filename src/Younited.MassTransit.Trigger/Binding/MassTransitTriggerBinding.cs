using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Younited.MassTransit.Trigger.Binding
{
    internal class MassTransitTriggerBinding<TMessage> : ITriggerBinding
        where TMessage : class
    {
        private readonly Dictionary<string, Type> _bindingDataContract;

        private IMassTransitListenerFactory MassTransitListenerFactory { get; }
        private string BusName { get; }
        private string QueueName { get; }
        public TriggerParameterMode TriggerParameterMode { get; }
        private SessionUsage SessionUsage { get; }
        private ParameterInfo Parameter { get; }

        public MassTransitTriggerBinding(IMassTransitListenerFactory listenerFactory, string busName, string queueName, TriggerParameterMode triggerParameterMode, SessionUsage sessionUsage, ParameterInfo parameter)
        {
            MassTransitListenerFactory = listenerFactory;
            BusName = busName;
            QueueName = queueName;
            TriggerParameterMode = triggerParameterMode;
            Parameter = parameter;
            SessionUsage = sessionUsage;
            _bindingDataContract = GetBindingDataContract(parameter);
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            IValueProvider valueProvider;
            switch (TriggerParameterMode)
            {
                case TriggerParameterMode.Message:
                    var message = value as TMessage;
                    valueProvider = new MessageBinder(message);
                    break;
                case TriggerParameterMode.ConsumeContext:
                    var consumeContext = value as ConsumeContext<TMessage>;
                    valueProvider = new ConsumeContextBinder(consumeContext);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var bindingData = CreateBindingData(Parameter, valueProvider);

            return Task.FromResult((ITriggerData)new TriggerData(valueProvider, bindingData));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult((IListener)new MassTransitTriggerListener<TMessage>(MassTransitListenerFactory, BusName, QueueName, TriggerParameterMode, SessionUsage, context.Executor));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            var descriptor = new MassTransitTriggerParameterDescriptor
            {
                Name = Parameter.Name,
                Type = "MassTransitTrigger",
                DisplayHints = new ParameterDisplayHints
                {
                    Description = string.Format($"Reception of a {typeof(TMessage)} message")
                }
            };
            return descriptor;
        }

        public Type TriggerValueType => Parameter.ParameterType;

        public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingDataContract;

        private static Dictionary<string, Type> GetBindingDataContract(ParameterInfo parameter)
        {
            return new Dictionary<string, Type>();
        }

        private static Dictionary<string, object> CreateBindingData(ParameterInfo parameter, object parameterValue)
        {
            return new Dictionary<string, object>();
        }

        private sealed class MessageBinder : IValueBinder
        {
            private readonly TMessage _message;

            public MessageBinder(TMessage message)
            {
                _message = message;
            }

            public Type Type => typeof(TMessage);

            public Task<object> GetValueAsync() => Task.FromResult((object)_message);

            public string ToInvokeString() => null;

            public Task SetValueAsync(object value, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class ConsumeContextBinder : IValueBinder
        {
            private readonly ConsumeContext<TMessage> _context;

            public ConsumeContextBinder(ConsumeContext<TMessage> context)
            {
                _context = context;
            }

            public Type Type => typeof(ConsumeContext<TMessage>);

            public Task<object> GetValueAsync() => Task.FromResult((object)_context);

            public string ToInvokeString() => null;

            public Task SetValueAsync(object value, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class MassTransitTriggerParameterDescriptor : TriggerParameterDescriptor
        {
            public override string GetTriggerReason(IDictionary<string, string> arguments)
            {
                return string.Format($"Received message {typeof(TMessage)} at {DateTime.Now:o}");
            }
        }
    }
}
