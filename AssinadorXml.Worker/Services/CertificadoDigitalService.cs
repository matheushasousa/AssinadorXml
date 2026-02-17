using AssinadorXml.Worker.Domain;
using AssinadorXml.Worker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssinadorXml.Worker.Services;

public class CertificadoDigitalService(AppDbContext context)
{
    public async Task<CertificadoDigital> ObterPorCnpjAsync(string cnpj)
    {
        var certificado = await context.CertificadosDigitais
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Cnpj == cnpj);

        if (certificado == null)
            throw new Exception($"Certificado não encontrado para CNPJ {cnpj}");

        return certificado;
    }
}
