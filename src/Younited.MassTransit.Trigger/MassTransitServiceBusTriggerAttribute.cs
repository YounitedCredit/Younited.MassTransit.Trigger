using System;
using Microsoft.Azure.WebJobs.Description;

namespace Younited.MassTransit.Trigger
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class MassTransitServiceBusTriggerAttribute : Attribute
    {
        public MassTransitServiceBusTriggerAttribute(string bus, string queueName)
        {
            Bus = bus;
            QueueName = queueName;
        }

        public string QueueName { get; }
        public string Bus { get; }
        public bool UseSession { get; set; }
    }
}
