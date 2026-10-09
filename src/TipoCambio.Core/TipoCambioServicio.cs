namespace TipoCambio.Core;

/// <summary>
/// Punto de entrada de la API: usa lo guardado si ya existe y solo consulta las fuentes externas cuando falta.
/// </summary>
public sealed class TipoCambioServicio(ITipoCambioProveedor proveedor, ITipoCambioRepositorio repositorio, TimeProvider reloj)
{
    public const int MaxDiasHistorico = 366;

    public async Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default)
    {
        var hoy = FechaPeru.Hoy(reloj);

        var guardado = await repositorio.ObtenerAsync(hoy, cancellationToken);
        if (guardado is not null)
            return guardado;

        // El respaldo puede devolver un día anterior (BCRP publica con retraso). Se guarda con su fecha real,
        // así hoy sigue sin dato y la siguiente consulta vuelve a intentar con la fuente principal.
        var obtenido = await proveedor.ObtenerHoyAsync(cancellationToken);
        await repositorio.GuardarSiNoExisteAsync(obtenido, cancellationToken);
        return obtenido;
    }

    public Task<IReadOnlyList<TipoCambioDia>> ObtenerHistoricoAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        var error = ValidarRango(desde, hasta);
        if (error is not null)
            throw new ArgumentException(error);

        return repositorio.ListarAsync(desde, hasta, cancellationToken);
    }

    /// <summary>Devuelve el motivo por el que el rango no es válido, o null si es válido.</summary>
    public static string? ValidarRango(DateOnly desde, DateOnly hasta)
    {
        if (desde > hasta)
            return "'desde' no puede ser posterior a 'hasta'.";

        if (hasta.DayNumber - desde.DayNumber + 1 > MaxDiasHistorico)
            return $"El rango no puede superar {MaxDiasHistorico} días.";

        return null;
    }
}
