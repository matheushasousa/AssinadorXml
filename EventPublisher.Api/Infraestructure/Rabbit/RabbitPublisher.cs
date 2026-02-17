using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EventPublisher.Api.Infraestructure.Rabbit;

public class RabbitPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["Rabbit:Host"],
            UserName = configuration["Rabbit:User"],
            Password = configuration["Rabbit:Password"]
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: "xml.events",
            type: ExchangeType.Topic,
            durable: true
        ).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(string routingKey, object message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(
            exchange: "xml.events",
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
