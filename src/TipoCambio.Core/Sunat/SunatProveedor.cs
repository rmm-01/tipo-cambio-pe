namespace TipoCambio.Core.Sunat;

/// <summary>Lee el tipo de cambio del día desde el TXT público de SUNAT.</summary>
public sealed class SunatProveedor(HttpClient http) : ITipoCambioProveedor
{
    // Ruta relativa a la BaseAddress configurada en el HttpClient.
    public const string RutaTxt = "a/txt/tipoCambio.txt";

    public async Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default)
    {
        string contenido;
        try
        {
            contenido = await http.GetStringAsync(RutaTxt, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ProveedorNoDisponibleException(SunatTxtParser.Fuente, "no respondió correctamente.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Cancelación sin que la pidiera el llamador = se agotó el tiempo de espera del HttpClient.
            throw new ProveedorNoDisponibleException(SunatTxtParser.Fuente, "tiempo de espera agotado.", ex);
        }

        try
        {
            return SunatTxtParser.Parse(contenido);
        }
        catch (FormatException ex)
        {
            throw new ProveedorNoDisponibleException(SunatTxtParser.Fuente, "respuesta con formato inesperado.", ex);
        }
    }
}
