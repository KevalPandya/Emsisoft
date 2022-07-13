using RabbitMQ.Client.Events;

namespace Processor.Interfaces
{
    public interface ISubscriber
    {
        void Start();
        event AsyncEventHandler<SubscriberEventArgs> OnMessage;
    }
}
