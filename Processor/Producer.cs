using Common.Models;
using Processor.Interfaces;
using System.Threading.Channels;

namespace Processor
{
    public class Producer : IProducer
    {
        private readonly ChannelWriter<Hashes> _writer;
        private readonly ILogger<Producer> _logger;

        public Producer(ChannelWriter<Hashes> writer, ILogger<Producer> logger)
        {
            _writer = writer;
            _logger = logger;
        }

        public async Task PublishAsync(Hashes hashes, CancellationToken cancellationToken = default)
        {
            await _writer.WriteAsync(hashes, cancellationToken);
            _logger.LogInformation($"Producer - Published hash on {hashes.Date} : '{hashes.SHA1}'");
        }
    }
}
