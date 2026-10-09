namespace TipoCambio.Core;

/// <summary>Fuente externa del tipo de cambio del día (SUNAT, BCRP, ...).</summary>
public interface ITipoCambioProveedor
{
    Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default);
}

/// <summary>La fuente externa no respondió o devolvió algo que no se pudo interpretar.</summary>
public sealed class ProveedorNoDisponibleException(string fuente, string mensaje, Exception? inner = null)
    : Exception($"{fuente}: {mensaje}", inner)
{
    public string Fuente { get; } = fuente;
}
