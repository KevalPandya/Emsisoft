using Processor.Interfaces;

namespace Processor
{
    public class BackgroundWorker : BackgroundService
    {
        private readonly ISubscriber _subscriber;
        private readonly ILogger<BackgroundWorker> _logger;

        private readonly IProducer _producer;
        private readonly IEnumerable<IConsumer> _consumers;

        public BackgroundWorker(ISubscriber subscriber, IProducer producer, IEnumerable<IConsumer> consumers, ILogger<BackgroundWorker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _subscriber.OnMessage += OnMessageAsync;

            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _consumers = consumers ?? Enumerable.Empty<Consumer>();
        }

        private async Task OnMessageAsync(object sender, SubscriberEventArgs args)
        {
            _logger.LogInformation($"got a new message: {args.Hashes.SHA1} at {args.Hashes.Date}");

            await _producer.PublishAsync(args.Hashes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _subscriber.Start();

            var consumerTasks = _consumers.Select(c => c.BeginConsumeAsync(stoppingToken));
            await Task.WhenAll(consumerTasks);
        }
    }
}
