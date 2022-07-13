using Common.Models;

namespace Processor
{
    public class SubscriberEventArgs : EventArgs
    {
        public SubscriberEventArgs(Hashes hashes)
        {
            this.Hashes = hashes;
        }

        public Hashes Hashes { get; }
    }
}
