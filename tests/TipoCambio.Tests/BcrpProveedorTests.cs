using System.Net;
using TipoCambio.Core;
using TipoCambio.Core.Bcrp;
using TipoCambio.Tests.Fakes;
using static TipoCambio.Tests.Fakes.HandlerFalso;

namespace TipoCambio.Tests;

public class BcrpProveedorTests
{
    private const string ConDatos = """{"periods":[{"name":"06.Oct.26","values":["3.431","3.437"]},{"name":"07.Oct.26","values":["n.d.","n.d."]}]}""";

    [Fact]
    public async Task ObtenerHoy_DevuelveUltimoDiaConDato()
    {
        var (proveedor, _) = CrearProveedor(_ => Respuesta(HttpStatusCode.OK, ConDatos));

        var resultado = await proveedor.ObtenerHoyAsync();

        Assert.Equal(new DateOnly(2026, 10, 6), resultado.Fecha);
        Assert.Equal("BCRP", resultado.Fuente);
    }

    [Fact]
    public async Task ObtenerHoy_ConsultaRangoSegunFechaDePeru()
    {
        // 02:00 UTC del 10/10 todavía es 09/10 en Lima (UTC-5).
        var (proveedor, handler) = CrearProveedor(
            _ => Respuesta(HttpStatusCode.OK, ConDatos),
            new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero));

        await proveedor.ObtenerHoyAsync();

        Assert.Equal(
            "/estadisticas/series/api/PD04639PD-PD04640PD/json/2026-09-29/2026-10-09",
            handler.UltimaPeticion!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ObtenerHoy_SinDatosEnElRango_LanzaProveedorNoDisponible()
    {
        var (proveedor, _) = CrearProveedor(
            _ => Respuesta(HttpStatusCode.OK, """{"periods":[{"name":"07.Oct.26","values":["n.d.","n.d."]}]}"""));

        var ex = await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
        Assert.Equal("BCRP", ex.Fuente);
    }

    [Fact]
    public async Task ObtenerHoy_ErrorHttp_LanzaProveedorNoDisponible()
    {
        var (proveedor, _) = CrearProveedor(_ => Respuesta(HttpStatusCode.ServiceUnavailable, ""));

        await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
    }

    [Fact]
    public async Task ObtenerHoy_RespuestaNoJson_LanzaProveedorNoDisponible()
    {
        var (proveedor, _) = CrearProveedor(_ => Respuesta(HttpStatusCode.OK, "<html>Error</html>"));

        await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
    }

    [Fact]
    public async Task ObtenerHoy_TiempoAgotado_LanzaProveedorNoDisponible()
    {
        var (proveedor, _) = CrearProveedor(_ => throw new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
    }

    private static (BcrpProveedor, HandlerFalso) CrearProveedor(
        Func<HttpRequestMessage, HttpResponseMessage> responder, DateTimeOffset? ahora = null)
    {
        var handler = new HandlerFalso(responder);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://bcrp.test/") };
        var reloj = new RelojFijo(ahora ?? new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.FromHours(-5)));
        return (new BcrpProveedor(http, reloj), handler);
    }
}
