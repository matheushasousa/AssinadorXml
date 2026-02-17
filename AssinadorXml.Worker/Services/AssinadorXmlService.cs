using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace AssinadorXml.Worker.Services;

public class AssinadorXmlService(
    CertificadoDigitalService certificadoDigitalService,
    IHttpClientFactory factory)
{
    private static readonly Dictionary<string, X509Certificate2> _cacheCertificates = new();

    public async Task ProcessarAsync(string eventId, string cnpj, string chave, string xml)
    {
        var certificado = await ObterCertificadoAsync(cnpj);

        var xmlAssinado = ExecuteAssinaturaXml(certificado, "NFe" + chave, xml);

        //await PublicarXmlAssinadoAsync(eventId, xmlAssinado);
    }

    private async Task<X509Certificate2> ObterCertificadoAsync(string cnpj)
    {
        if (_cacheCertificates.TryGetValue(cnpj, out var certificado))
            return certificado;

        var certificadoDigital = await certificadoDigitalService.ObterPorCnpjAsync(cnpj);

        var certificate = new X509Certificate2(
            certificadoDigital.ArquivoPfx,
            certificadoDigital.Senha,
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.Exportable
        );

        _cacheCertificates[cnpj] = certificate;

        return certificate;
    }

    private string ExecuteAssinaturaXml(X509Certificate2 certificate, string id, string xml)
    {
        var xmlDocumento = new XmlDocument { PreserveWhitespace = true };
        xmlDocumento.LoadXml(xml);

        var signedXml = new SignedXml(xmlDocumento) { SigningKey = certificate.GetRSAPrivateKey() };

        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        var reference = new Reference { Uri = "#" + id, DigestMethod = SignedXml.XmlDsigSHA256Url };

        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());

        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));

        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();

        var xmlDigitalSignature = signedXml.GetXml();

        return xml + xmlDigitalSignature.OuterXml;
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
