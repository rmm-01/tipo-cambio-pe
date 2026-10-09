namespace TipoCambio.Core.Bcrp;

/// <summary>
/// Lee el tipo de cambio SBS (compra/venta) desde la API pública de BCRPData.
/// El BCRP publica con unos días de retraso, así que devuelve el último día con dato,
/// no necesariamente el de hoy: el campo <see cref="TipoCambioDia.Fecha"/> indica cuál es.
/// </summary>
public sealed class BcrpProveedor(HttpClient http, TimeProvider reloj) : ITipoCambioProveedor
{
    // PD04639PD = TC Sistema bancario SBS compra; PD04640PD = venta.
    public const string Series = "PD04639PD-PD04640PD";

    // Cubre fines de semana largos y feriados sin publicación.
    public const int DiasHaciaAtras = 10;

    // Perú no tiene horario de verano: UTC-5 todo el año.
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    public async Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default)
    {
        var hasta = DateOnly.FromDateTime(reloj.GetUtcNow().ToOffset(HoraPeru).DateTime);
        var desde = hasta.AddDays(-DiasHaciaAtras);
        var ruta = $"estadisticas/series/api/{Series}/json/{desde:yyyy-MM-dd}/{hasta:yyyy-MM-dd}";

        string contenido;
        try
        {
            contenido = await http.GetStringAsync(ruta, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ProveedorNoDisponibleException(BcrpJsonParser.Fuente, "no respondió correctamente.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProveedorNoDisponibleException(BcrpJsonParser.Fuente, "tiempo de espera agotado.", ex);
        }

        IReadOnlyList<TipoCambioDia> dias;
        try
        {
            dias = BcrpJsonParser.Parse(contenido);
        }
        catch (FormatException ex)
        {
            throw new ProveedorNoDisponibleException(BcrpJsonParser.Fuente, "respuesta con formato inesperado.", ex);
        }

        return dias.Count > 0
            ? dias[^1]
            : throw new ProveedorNoDisponibleException(BcrpJsonParser.Fuente, $"sin datos publicados en los últimos {DiasHaciaAtras} días.");
    }
}
