using Common;
using Common.Models;
using Processor.Interfaces;

namespace Processor
{
    public class MessagesRepository : IMessagesRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public MessagesRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Add(Hashes hashes)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                _context.Hashes.Add(hashes ?? throw new ArgumentNullException(nameof(hashes)));
                _context.SaveChanges();
            }
        }
    }
}
