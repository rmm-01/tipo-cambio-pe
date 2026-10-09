using System.Globalization;

namespace TipoCambio.Core.Sunat;

/// <summary>
/// Interpreta el TXT público de SUNAT, con formato <c>dd/MM/yyyy|compra|venta|</c>.
/// </summary>
public static class SunatTxtParser
{
    public const string Fuente = "SUNAT";

    public static TipoCambioDia Parse(string contenido)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        // El TXT termina en '|' y a veces trae salto de línea; se ignoran los campos vacíos.
        var campos = contenido.Trim().Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (campos.Length != 3)
            throw new FormatException($"Se esperaban 3 campos (fecha|compra|venta) y llegaron {campos.Length}.");

        if (!DateOnly.TryParseExact(campos[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            throw new FormatException($"Fecha inválida: '{campos[0]}'.");

        var compra = ParseMonto(campos[1], "compra");
        var venta = ParseMonto(campos[2], "venta");

        return new TipoCambioDia(fecha, compra, venta, Fuente);
    }

    private static decimal ParseMonto(string valor, string nombre)
    {
        // InvariantCulture: SUNAT usa punto decimal y la cultura es-PE de la máquina podría esperar coma.
        if (!decimal.TryParse(valor, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var monto) || monto <= 0)
            throw new FormatException($"Valor de {nombre} inválido: '{valor}'.");

        return monto;
    }
}
