namespace TipoCambio.Tests.Fakes;

/// <summary>Reloj detenido en un instante dado, para que las pruebas no dependan de la fecha real.</summary>
internal sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora.ToUniversalTime();
}
