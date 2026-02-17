using System.ComponentModel.DataAnnotations.Schema;

namespace AssinadorXml.Worker.Domain;

[Table("certificado_digital")]
public class CertificadoDigital
{
    [Column("id")]
    public int Id { get; set; }
    [Column("cnpj")]
    public required string Cnpj { get; set; }
    [Column("arquivo_pfx")]
    public required byte[] ArquivoPfx { get; set; }
    [Column("senha")]
    public required string Senha { get; set; }
}
