using Common.Interfaces;
using RabbitMQ.Client;

namespace Common
{
    public class SharedConnection: IDisposable, ISharedConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private bool _disposed;

        private readonly object semaphore = new object();

        public SharedConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IModel CreateChannel()
        {
            TryConnect();

            if (!IsConnected)
                throw new InvalidOperationException("RabbitMQ connection is not open.");

            return _connection.CreateModel();
        }

        private void TryConnect()
        {
            lock (semaphore)
            {
                if (IsConnected)
                    return;

                try
                {
                    _connection = _connectionFactory.CreateConnection();
                    _connection.ConnectionShutdown += (s, e) => TryConnect();
                    _connection.CallbackException += (s, e) => TryConnect();
                    _connection.ConnectionBlocked += (s, e) => TryConnect();
                }
                catch
                {
                    throw new InvalidOperationException("Cannot connect to RabbitMQ.");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _connection.Dispose();
        }
    }
}
