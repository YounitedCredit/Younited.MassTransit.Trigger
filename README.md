# Younited Credit MassTransit Function Trigger

The purpose of this project is to allow a function to configure a trigger on an Azure Service Bus queue through MassTransit library.

## Usage

### Define the function trigger
```cs
[FunctionName("MyFunction")]
public static async Task MyFunction(
    [MassTransitServiceBusTrigger("bus", "queue")] Message message,
    ILogger log)
{
    log.LogInformation($"Received a message!");
}
```
or, if you prefer to use the ConsumeContext features:
```cs
[FunctionName("MyFunction")]
public static async Task MyFunction(
    [MassTransitServiceBusTrigger("bus", "queue")] ConsumeContext<Message> context,
    ILogger log)
{
    log.LogInformation($"Received a message!");
}
```

The `"bus"` parameter is a logical name you will use to refer to the service bus connection (see below).
The `"queue` parameter indicates which queue will be used to forward subscription messages and store them if the function is unable to process them.

### Connect and configure MassTransit
Create a startup class to provide configuration related to MassTransit through code.
```cs
[assembly: WebJobsStartup(typeof(Infrastructure.FunctionsStartup))]

namespace Infrastructure
{
    public class FunctionsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var factory = new ServiceBusHostConfigurationFactory();
            factory.RegisterHostConfiguration(
                "bus", // this is the logical name you used to refer to the connection in the trigger
                new Uri("sb://test.servicebus.windows.net/namespace"),
                cfg => cfg.SharedAccessSignature(s =>
                {
                    s.TokenScope = TokenScope.Namespace;
                    s.TokenTimeToLive = TimeSpan.FromDays(1);
                    s.KeyName = "RootManageSharedAccessKey";
                    s.SharedAccessKey = "accesskey";
                })
            );
            // configure trigger for the specific messages types you want to customize
            factory.RegisterTriggerConfiguration<Message>(cfg => cfg.UseMessageRetry(r => r.Immediate(5)));
            builder.Services.AddSingleton<IServiceBusHostConfigurationFactory>(factory);
        }
    }
}
```

## Thanks
Special thanks to [Anthony HOCQUET] for his help on building and testing this library.

[Anthony HOCQUET]: https://github.com/ahocquet