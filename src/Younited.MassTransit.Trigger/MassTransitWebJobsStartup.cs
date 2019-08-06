using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Younited.MassTransit.Trigger;
using Younited.MassTransit.Trigger.Config;

[assembly: WebJobsStartup(typeof(MassTransitWebJobsStartup))]

namespace Younited.MassTransit.Trigger
{
    internal class MassTransitWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddMassTransit();
        }
    }
}
