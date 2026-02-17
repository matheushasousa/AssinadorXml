using EventPublisher.Api.Application.DTOs;
using EventPublisher.Api.Infraestructure.Rabbit;
using Microsoft.AspNetCore.Mvc;

namespace EventPublisher.Api.Controllers;

[ApiController]
[Route("api/xml-assinado")]
public class XmlAssinadoController(RabbitPublisher publisher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(XmlAssinadoInputDTO input)
    {
        await publisher.PublishAsync("xml.assinado", new
        {
            eventId = Guid.NewGuid(),
            eventType = "XML_ASSINADO",
            eventDateTime = DateTime.UtcNow,
            parentEventId = input.ParentEventId,
            xml = input.Xml,
        });

        return Ok();
    }
}
