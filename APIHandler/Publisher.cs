using Common.Interfaces;
using Common.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace APIHandler
{
    public class Publisher : IDisposable
    {
        private readonly ISharedConnection _sharedConnection;
        private readonly string _exchangeName;
        private IModel _channel;
        private readonly IBasicProperties _properties;

        public Publisher(ISharedConnection sharedConnection, string exchangeName)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException($"'{nameof(exchangeName)}' should not be blank.", nameof(exchangeName));

            _sharedConnection = sharedConnection ?? throw new ArgumentNullException(nameof(sharedConnection));

            _exchangeName = exchangeName;

            _channel = _sharedConnection.CreateChannel();
            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Fanout);

            _properties = _channel.CreateBasicProperties();
        }

        public void Publish(Hashes hashes)
        {
            var jsonData = JsonSerializer.Serialize(hashes);

            var body = Encoding.UTF8.GetBytes(jsonData);

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: string.Empty,
                mandatory: true,
                basicProperties: _properties,
                body: body);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _channel = null;
        }
    }
}
