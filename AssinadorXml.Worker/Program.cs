using AssinadorXml.Worker.Infrastructure.Data;
using AssinadorXml.Worker.Infrastructure.Messaging;
using AssinadorXml.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AssinadorXmlService>();
builder.Services.AddScoped<CertificadoDigitalService>();

builder.Services.AddHostedService<RabbitConsumerService>();

builder.Services.AddHttpClient("EventPublisher", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["EventPublisher:BaseUrl"]!
    );
});

var app = builder.Build();
app.Run();