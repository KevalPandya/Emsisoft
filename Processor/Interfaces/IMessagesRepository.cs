using Common.Models;

namespace Processor.Interfaces
{
    public interface IMessagesRepository
    {
        void Add(Hashes hashes);
    }
}
