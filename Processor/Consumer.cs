using Common.Models;
using Processor.Interfaces;
using System.Threading.Channels;

namespace Processor
{
    public class Consumer : IConsumer
    {
        private readonly ChannelReader<Hashes> _reader;
        private readonly ILogger<Consumer> _logger;

        private readonly IMessagesRepository _messagesRepository;
        private readonly int _instanceId;

        public Consumer(ChannelReader<Hashes> reader, ILogger<Consumer> logger, int instanceId, IMessagesRepository messagesRepository)
        {
            _reader = reader;
            _instanceId = instanceId;
            _logger = logger;
            _messagesRepository = messagesRepository;
        }

        public async Task BeginConsumeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Consumer {_instanceId} - Starting");

            try
            {
                await foreach (var hash in _reader.ReadAllAsync(cancellationToken))
                {
                    _logger.LogInformation($"CONSUMER ({_instanceId}) - Received hash on {hash.Date} : '{hash.SHA1}'");
                    await Task.Delay(500, cancellationToken);
                    _messagesRepository.Add(hash);
                }
            }
            catch
            {
                _logger.LogWarning($"Consumer {_instanceId} - Forced Stop");
            }

            _logger.LogInformation($"Consumer {_instanceId} - Shutting Down");
        }

    }
}
