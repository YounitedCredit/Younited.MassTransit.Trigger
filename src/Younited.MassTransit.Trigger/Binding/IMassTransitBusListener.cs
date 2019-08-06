using System.Threading;
using System.Threading.Tasks;

namespace Younited.MassTransit.Trigger.Binding
{
    public interface IMassTransitBusListener
    {
        /// <summary>
        /// Starts the bus (assuming the battery isn't dead). Once the bus has been started, it cannot be started again, even after is has been stopped.
        /// </summary>
        /// <returns>The BusHandle for the started bus. This is no longer needed, as calling Stop on the IBusControl will stop the bus equally well.</returns>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the bus if it has been started. If the bus hasn't been started, the method returns without any warning.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
