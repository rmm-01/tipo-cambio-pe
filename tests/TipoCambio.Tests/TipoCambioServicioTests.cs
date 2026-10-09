using TipoCambio.Core;
using TipoCambio.Datos;
using TipoCambio.Tests.Fakes;

namespace TipoCambio.Tests;

public sealed class TipoCambioServicioTests : IDisposable
{
    private static readonly DateOnly Hoy = new(2026, 10, 9);
    private static readonly TipoCambioDia DeSunat = new(Hoy, 3.446m, 3.453m, "SUNAT");
    private static readonly TipoCambioDia DeBcrp = new(new DateOnly(2026, 10, 6), 3.431m, 3.437m, "BCRP");

    private readonly BaseEnMemoria _base = new();
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.FromHours(-5)));

    [Fact]
    public async Task ObtenerHoy_SinDatoGuardado_ConsultaYGuarda()
    {
        var proveedor = new ProveedorFalso(() => DeSunat);

        var resultado = await Servicio(proveedor).ObtenerHoyAsync();

        Assert.Equal(DeSunat, resultado);
        Assert.Equal(DeSunat, await Repositorio().ObtenerAsync(Hoy));
    }

    [Fact]
    public async Task ObtenerHoy_ConDatoGuardado_NoConsultaFuenteExterna()
    {
        var proveedor = new ProveedorFalso(() => DeSunat);
        await Servicio(proveedor).ObtenerHoyAsync();

        await Servicio(proveedor).ObtenerHoyAsync();
        await Servicio(proveedor).ObtenerHoyAsync();

        Assert.Equal(1, proveedor.Llamadas);
    }

    [Fact]
    public async Task ObtenerHoy_RespaldoDevuelveDiaAnterior_LoGuardaConSuFechaYReintentaLuego()
    {
        var proveedor = new ProveedorFalso(() => DeBcrp);

        var primero = await Servicio(proveedor).ObtenerHoyAsync();
        await Servicio(proveedor).ObtenerHoyAsync();

        Assert.Equal(DeBcrp, primero);
        Assert.Equal(DeBcrp, await Repositorio().ObtenerAsync(DeBcrp.Fecha));
        Assert.Null(await Repositorio().ObtenerAsync(Hoy));
        Assert.Equal(2, proveedor.Llamadas);
    }

    [Fact]
    public async Task ObtenerHoy_FuentesNoDisponibles_NoGuardaNada()
    {
        var proveedor = new ProveedorFalso(() => throw new ProveedorNoDisponibleException("SUNAT y BCRP", "caídas"));

        await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => Servicio(proveedor).ObtenerHoyAsync());
        Assert.Empty(await Repositorio().ListarAsync(Hoy.AddDays(-30), Hoy));
    }

    [Fact]
    public async Task ObtenerHistorico_RangoInvalido_LanzaArgumentException()
    {
        var servicio = Servicio(new ProveedorFalso(() => DeSunat));

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ObtenerHistoricoAsync(Hoy, Hoy.AddDays(-1)));
    }

    [Theory]
    [InlineData("2026-10-01", "2026-10-31", true)]
    [InlineData("2026-10-09", "2026-10-09", true)]
    [InlineData("2026-01-01", "2026-12-31", true)]
    [InlineData("2024-01-01", "2024-12-31", true)]
    [InlineData("2025-01-01", "2026-01-02", false)]
    [InlineData("2026-10-10", "2026-10-09", false)]
    public void ValidarRango(string desde, string hasta, bool esValido)
    {
        var error = TipoCambioServicio.ValidarRango(DateOnly.Parse(desde), DateOnly.Parse(hasta));

        Assert.Equal(esValido, error is null);
    }

    public void Dispose() => _base.Dispose();

    private TipoCambioRepositorio Repositorio() => new(_base.CrearContexto(), _reloj);

    private TipoCambioServicio Servicio(ITipoCambioProveedor proveedor) => new(proveedor, Repositorio(), _reloj);

    private sealed class ProveedorFalso(Func<TipoCambioDia> obtener) : ITipoCambioProveedor
    {
        public int Llamadas { get; private set; }

        public Task<TipoCambioDia> ObtenerHoyAsync(CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Task.FromResult(obtener());
        }
    }
}
