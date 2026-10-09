using System.Globalization;
using System.Text.Json;

namespace TipoCambio.Core.Bcrp;

/// <summary>
/// Interpreta la respuesta JSON de la API de BCRPData para dos series (compra y venta).
/// Cada periodo viene como <c>{"name":"06.Oct.26","values":["3.431","3.437"]}</c>.
/// </summary>
public static class BcrpJsonParser
{
    public const string Fuente = "BCRP";

    // El BCRP abrevia los meses en español y usa "Set" para septiembre.
    private static readonly Dictionary<string, int> Meses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ene"] = 1, ["Feb"] = 2, ["Mar"] = 3, ["Abr"] = 4, ["May"] = 5, ["Jun"] = 6,
        ["Jul"] = 7, ["Ago"] = 8, ["Set"] = 9, ["Sep"] = 9, ["Oct"] = 10, ["Nov"] = 11, ["Dic"] = 12,
    };

    /// <summary>Devuelve los días con dato, en orden cronológico; omite los marcados como "n.d.".</summary>
    public static IReadOnlyList<TipoCambioDia> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException("La respuesta no es un JSON válido.", ex);
        }

        using (documento)
        {
            if (!documento.RootElement.TryGetProperty("periods", out var periodos) || periodos.ValueKind != JsonValueKind.Array)
                throw new FormatException("La respuesta no contiene la lista 'periods'.");

            var dias = new List<TipoCambioDia>();
            foreach (var periodo in periodos.EnumerateArray())
            {
                var nombre = periodo.GetProperty("name").GetString() ?? "";
                var valores = periodo.GetProperty("values");
                if (valores.GetArrayLength() != 2)
                    throw new FormatException($"Se esperaban 2 valores (compra, venta) en '{nombre}'.");

                // "n.d." = el BCRP aún no publica ese día; no es un error.
                if (!TryParseMonto(valores[0].GetString(), out var compra) || !TryParseMonto(valores[1].GetString(), out var venta))
                    continue;

                dias.Add(new TipoCambioDia(ParseFecha(nombre), compra, venta, Fuente));
            }

            return dias.OrderBy(d => d.Fecha).ToList();
        }
    }

    private static DateOnly ParseFecha(string nombre)
    {
        var partes = nombre.Split('.');
        if (partes.Length == 3
            && int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var dia)
            && Meses.TryGetValue(partes[1], out var mes)
            && int.TryParse(partes[2], NumberStyles.None, CultureInfo.InvariantCulture, out var anio)
            && partes[2].Length == 2)
        {
            try
            {
                return new DateOnly(2000 + anio, mes, dia);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Cae al FormatException de abajo (por ejemplo, "31.Feb.26").
            }
        }

        throw new FormatException($"Fecha de periodo inválida: '{nombre}'.");
    }

    private static bool TryParseMonto(string? valor, out decimal monto) =>
        decimal.TryParse(valor, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out monto) && monto > 0;
}
