namespace TipoCambio.Core;

/// <summary>Fecha calendario en Perú, independiente de la zona horaria del servidor.</summary>
public static class FechaPeru
{
    // Perú no tiene horario de verano: UTC-5 todo el año.
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateOnly Hoy(TimeProvider reloj) =>
        DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(Desfase).DateTime);
}
