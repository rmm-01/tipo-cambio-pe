namespace TipoCambio.Core;

/// <summary>Tipo de cambio oficial (soles por dólar) publicado para una fecha.</summary>
public sealed record TipoCambioDia(DateOnly Fecha, decimal Compra, decimal Venta, string Fuente);
