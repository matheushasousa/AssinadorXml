using AssinadorXml.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AssinadorXml.Worker.Infrastructure.Messaging;

public class RabbitConsumerService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitConsumerService> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    private Stopwatch _stopwatch = new Stopwatch();
    private int _messagesConsumed = 0;
    private object _lock = new object();

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
            var startProcessing = DateTime.UtcNow;

            lock (_lock)
            {
                if (!_stopwatch.IsRunning)
                {
                    _stopwatch.Start();
                    logger.LogInformation("Iniciando contagem de tempo para o lote de mensagens.");
                }
            }

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

            var duration = DateTime.UtcNow - startProcessing;
            lock (_lock)
            {
                _messagesConsumed++;
            }

            logger.LogInformation(
                    "Mensagem consumida com sucesso! EventId={EventId}, TempoProcessamento={Duration}ms, TotalMensagens={Total}",
                    eventId,
                    duration.TotalMilliseconds,
                    _messagesConsumed
                );

            logger.LogInformation(
                "Tempo total acumulado até agora: {ElapsedMilliseconds} ms",
                _stopwatch.ElapsedMilliseconds
            );
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

        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            logger.LogInformation(
                "Tempo total para processamento do lote de mensagens: {TotalSeconds} segundos",
                _stopwatch.Elapsed.TotalSeconds
            );
        }

        await base.StopAsync(cancellationToken);
    }
}
