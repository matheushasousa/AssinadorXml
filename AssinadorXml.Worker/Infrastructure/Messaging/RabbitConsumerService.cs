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
    private DateTime _lastMessageProcessed = DateTime.UtcNow;

    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(1);

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
            lock (_lock)
            {
                if (!_stopwatch.IsRunning)
                {
                    _stopwatch.Start();
                    logger.LogInformation("Iniciando contagem de tempo para o lote de mensagens.");
                }
            }

            CheckInactivity();

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

            lock (_lock)
            {
                _messagesConsumed++;
                _lastMessageProcessed = DateTime.UtcNow;
            }

            logger.LogInformation(
                "Total mensagens consumidas={MessagesConsumed}, Tempo total acumulado: {ElapsedMilliseconds} ms",
                _messagesConsumed,
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
                "Tempo total para processamento de {TotalMensagens} mensagens: {TotalSeconds} ms",
                _messagesConsumed,
                _stopwatch.ElapsedMilliseconds
            );
        }

        await base.StopAsync(cancellationToken);
    }

    private void CheckInactivity()
    {
        lock (_lock)
        {
            if (_stopwatch.IsRunning && DateTime.UtcNow - _lastMessageProcessed > _inactivityThreshold)
            {
                logger.LogInformation(
                    "Nenhuma mensagem recebida nos últimos {Minutes} minutos. Resetando métricas.",
                    _inactivityThreshold.TotalMinutes
                );

                _stopwatch.Reset();
                _messagesConsumed = 0;
            }
        }
    }
}
