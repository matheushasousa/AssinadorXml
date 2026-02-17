using EventPublisher.Api.Application.DTOs;
using EventPublisher.Api.Infraestructure.Rabbit;
using Microsoft.AspNetCore.Mvc;

namespace EventPublisher.Api.Controllers;

[ApiController]
[Route("api/xml-recebido")]
public class XmlRecebidoController(RabbitPublisher publisher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(XmlRecebidoInputDTO input)
    {
        await publisher.PublishAsync("xml.recebido", new
        {
            eventId = Guid.NewGuid(),
            eventType = "XML_RECEBIDO",
            eventDateTime = DateTime.UtcNow,
            cnpj = input.Cnpj,
            xml = input.Xml,
        });

        return Ok();
    }
}
