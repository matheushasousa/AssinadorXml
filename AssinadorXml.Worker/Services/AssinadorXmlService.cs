using System.Net.Http.Json;

namespace AssinadorXml.Worker.Services;

public class AssinadorXmlService(
    CertificadoDigitalService certificadoDigitalService,
    IHttpClientFactory factory)
{
    public async Task ProcessarAsync(string eventId, string cnpj, string xml)
    {
        var certificadoDigital = await certificadoDigitalService.ObterPorCnpjAsync(cnpj);

        //var certificate = new X509Certificate2(
        //    certificadoDigital.ArquivoPfx,
        //    certificadoDigital.Senha,
        //    X509KeyStorageFlags.MachineKeySet |
        //    X509KeyStorageFlags.Exportable
        //);

        // 🔐 Aqui entrará sua assinatura XML real (XmlDsig)

        var xmlAssinado = xml; // placeholder

        await PublicarXmlAssinadoAsync(eventId, xmlAssinado);
    }

    public async Task PublicarXmlAssinadoAsync(string eventId, string xmlAssinado)
    {
        var client = factory.CreateClient("EventPublisher");

        var response = await client.PostAsJsonAsync(
            "/api/xml-assinado",
            new
            {
                ParentEventId = eventId,
                Xml = xmlAssinado
            }
        );

        response.EnsureSuccessStatusCode();
    }
}
