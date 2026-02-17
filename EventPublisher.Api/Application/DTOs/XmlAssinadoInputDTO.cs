namespace EventPublisher.Api.Application.DTOs;

public class XmlAssinadoInputDTO
{
    public required string ParentEventId { get; set; }
    public required string Xml { get; set; }
}
