using AssinadorXml.Worker.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssinadorXml.Worker.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CertificadoDigital> CertificadosDigitais => Set<CertificadoDigital>();
}
