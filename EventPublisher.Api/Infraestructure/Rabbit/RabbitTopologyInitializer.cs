using RabbitMQ.Client;

namespace EventPublisher.Api.Infraestructure.Rabbit;

public class RabbitTopologyInitializer(IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["Rabbit:Host"],
            UserName = configuration["Rabbit:User"],
            Password = configuration["Rabbit:Password"]
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "xml.events",
            type: ExchangeType.Topic,
            durable: true
        );

        #region Xml recebido
        await channel.QueueDeclareAsync(
            queue: "xml.recebido.queue",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await channel.QueueBindAsync(
            queue: "xml.recebido.queue",
            exchange: "xml.events",
            routingKey: "xml.recebido"
        );
        #endregion

        #region Xml assinado
        await channel.QueueDeclareAsync(
            queue: "xml.assinado.queue",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await channel.QueueBindAsync(
            queue: "xml.assinado.queue",
            exchange: "xml.events",
            routingKey: "xml.assinado"
        );
        #endregion
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
