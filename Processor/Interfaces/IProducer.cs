using Common.Models;

namespace Processor.Interfaces
{
    public interface IProducer
    {
        Task PublishAsync(Hashes hashes, CancellationToken cancellationToken = default);
    }
}
