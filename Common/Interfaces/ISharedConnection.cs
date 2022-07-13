using RabbitMQ.Client;

namespace Common.Interfaces
{
    public interface ISharedConnection
    {
        bool IsConnected { get; }

        IModel CreateChannel();
    }
}