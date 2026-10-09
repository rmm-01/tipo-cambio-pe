namespace TipoCambio.Datos;

/// <summary>Fila de la tabla <c>TiposCambio</c>. Separada del modelo de dominio para no atarlo a EF Core.</summary>
public sealed class TipoCambioRegistro
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Compra { get; set; }
    public decimal Venta { get; set; }
    public required string Fuente { get; set; }
    public DateTimeOffset RegistradoEn { get; set; }
}
