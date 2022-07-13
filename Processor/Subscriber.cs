using Common.Interfaces;
using Common.Models;
using Processor.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Processor
{
    public record SubscriberOptions(string ExchangeName, string QueueName, string DeadLetterExchangeName, string DeadLetterQueue);

    public class Subscriber : ISubscriber, IDisposable
    {
        private readonly ISharedConnection _sharedConnection;
        private readonly SubscriberOptions _subscriberOptions;
        private readonly ILogger<Subscriber> _logger;
        private IModel _channel;

        public Subscriber(ISharedConnection sharedConnection, SubscriberOptions options, ILogger<Subscriber> logger)
        {
            _sharedConnection = sharedConnection ?? throw new ArgumentNullException(nameof(sharedConnection));
            _subscriberOptions = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event AsyncEventHandler<SubscriberEventArgs> OnMessage;

        public void Start()
        {
            InitChannel();
            InitSubscription();
        }

        private void InitChannel()
        {
            _channel?.Dispose();

            _channel = _sharedConnection.CreateChannel();

            _channel.ExchangeDeclare(exchange: _subscriberOptions.DeadLetterExchangeName, type: ExchangeType.Fanout);
            _channel.QueueDeclare(queue: _subscriberOptions.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(_subscriberOptions.DeadLetterQueue, _subscriberOptions.DeadLetterExchangeName, routingKey: string.Empty, arguments: null);

            _channel.ExchangeDeclare(exchange: _subscriberOptions.ExchangeName, type: ExchangeType.Fanout);

            _channel.QueueDeclare(queue: _subscriberOptions.QueueName,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: null);

            _channel.QueueBind(_subscriberOptions.QueueName, _subscriberOptions.ExchangeName, string.Empty, null);

            _channel.CallbackException += (sender, ea) =>
            {
                InitChannel();
                InitSubscription();
            };
        }

        private void InitSubscription()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(queue: _subscriberOptions.QueueName, autoAck: false, consumer: consumer);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            var consumer = sender as IBasicConsumer;
            var channel = consumer?.Model ?? _channel;

            Hashes hashes = null;

            try
            {
                var body = Encoding.UTF8.GetString(eventArgs.Body.Span);
                hashes = JsonSerializer.Deserialize<Hashes>(body);
                await this.OnMessage(this, new SubscriberEventArgs(hashes));
            }
            catch (Exception ex)
            {
                var errMsg = (hashes is null) ? $"an error has occurred while processing a hash: {ex.Message}"
                    : $"an error has occurred while processing hash '{hashes.SHA1}': {ex.Message}";
                _logger.LogError(ex, errMsg);

                if (eventArgs.Redelivered)
                    channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                else
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
