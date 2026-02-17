using AssinadorXml.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AssinadorXml.Worker.Infrastructure.Messaging;

public class RabbitConsumerService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"],
            UserName = configuration["RabbitMQ:User"],
            Password = configuration["RabbitMQ:Password"]
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        var queueName = configuration["RabbitMQ:Queue"];

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<JsonElement>(json);

            var eventId = message.GetProperty("eventId").GetString()!;
            var cnpj = message.GetProperty("cnpj").GetString()!;
            var chave = message.GetProperty("chave").GetString()!;
            var xml = message.GetProperty("xml").GetString()!;

            using var scope = scopeFactory.CreateScope();
            var signer = scope.ServiceProvider.GetRequiredService<AssinadorXmlService>();

            await signer.ProcessarAsync(eventId, cnpj, chave, xml);

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();

        await base.StopAsync(cancellationToken);
    }
}
