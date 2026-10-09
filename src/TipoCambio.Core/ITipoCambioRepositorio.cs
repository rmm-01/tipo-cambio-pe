namespace TipoCambio.Core;

/// <summary>Almacén de los tipos de cambio ya obtenidos, uno por fecha.</summary>
public interface ITipoCambioRepositorio
{
    Task<TipoCambioDia?> ObtenerAsync(DateOnly fecha, CancellationToken cancellationToken = default);

    /// <summary>Guarda el valor si esa fecha aún no existe; si ya existe, no hace nada.</summary>
    Task GuardarSiNoExisteAsync(TipoCambioDia tipoCambio, CancellationToken cancellationToken = default);

    /// <summary>Días guardados entre <paramref name="desde"/> y <paramref name="hasta"/> (ambos incluidos), en orden.</summary>
    Task<IReadOnlyList<TipoCambioDia>> ListarAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);
}
