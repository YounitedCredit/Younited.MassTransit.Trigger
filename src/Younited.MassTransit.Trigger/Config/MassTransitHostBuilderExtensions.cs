using System;
using Microsoft.Azure.WebJobs;

namespace Younited.MassTransit.Trigger.Config
{
    public static class MassTransitHostBuilderExtensions
    {
        public static IWebJobsBuilder AddMassTransit(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<MassTransitExtensionConfigProvider>();
            return builder;
        }
    }
}
