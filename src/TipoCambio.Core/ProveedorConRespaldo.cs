using Microsoft.Extensions.Logging;

namespace TipoCambio.Core;

/// <summary>
/// Consulta la fuente principal y, si no está disponible, la de respaldo.
/// Para quien lo usa es un proveedor más: no sabe que hay dos fuentes detrás.
/// </summary>
public sealed class ProveedorConRespaldo(
    ITipoCambioProveedor principal,
    ITipoCambioProveedor respaldo,
    ILogger<ProveedorConRespaldo> logger) : ITipoCambioProveedor
{
    public async Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await principal.ObtenerHoyAsync(cancellationToken);
        }
        catch (ProveedorNoDisponibleException exPrincipal)
        {
            logger.LogWarning(exPrincipal, "Fuente principal no disponible ({Fuente}); se usa el respaldo.", exPrincipal.Fuente);

            try
            {
                return await respaldo.ObtenerHoyAsync(cancellationToken);
            }
            catch (ProveedorNoDisponibleException exRespaldo)
            {
                throw new ProveedorNoDisponibleException(
                    $"{exPrincipal.Fuente} y {exRespaldo.Fuente}",
                    "ninguna fuente está disponible.",
                    new AggregateException(exPrincipal, exRespaldo));
            }
        }
    }
}
