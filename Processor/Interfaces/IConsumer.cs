namespace Processor.Interfaces
{
    public interface IConsumer
    {
        Task BeginConsumeAsync(CancellationToken cancellationToken = default);
    }
}
