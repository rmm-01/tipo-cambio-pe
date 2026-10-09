using System.Net;
using TipoCambio.Core;
using TipoCambio.Core.Sunat;

namespace TipoCambio.Tests;

public class SunatProveedorTests
{
    [Fact]
    public async Task ObtenerHoy_RespuestaValida_DevuelveTipoCambio()
    {
        var proveedor = CrearProveedor(_ => Respuesta(HttpStatusCode.OK, "09/10/2026|3.446|3.453|"));

        var resultado = await proveedor.ObtenerHoyAsync();

        Assert.Equal(3.453m, resultado.Venta);
    }

    [Fact]
    public async Task ObtenerHoy_ErrorHttp_LanzaProveedorNoDisponible()
    {
        var proveedor = CrearProveedor(_ => Respuesta(HttpStatusCode.InternalServerError, ""));

        var ex = await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
        Assert.Equal("SUNAT", ex.Fuente);
    }

    [Fact]
    public async Task ObtenerHoy_FormatoInesperado_LanzaProveedorNoDisponible()
    {
        var proveedor = CrearProveedor(_ => Respuesta(HttpStatusCode.OK, "<html>Mantenimiento</html>"));

        await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
    }

    [Fact]
    public async Task ObtenerHoy_TiempoAgotado_LanzaProveedorNoDisponible()
    {
        var proveedor = CrearProveedor(_ => throw new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ProveedorNoDisponibleException>(() => proveedor.ObtenerHoyAsync());
    }

    [Fact]
    public async Task ObtenerHoy_CanceladoPorElLlamador_PropagaCancelacion()
    {
        var proveedor = CrearProveedor(_ => Respuesta(HttpStatusCode.OK, "09/10/2026|3.446|3.453|"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => proveedor.ObtenerHoyAsync(cts.Token));
    }

    private static SunatProveedor CrearProveedor(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new HandlerFalso(responder)) { BaseAddress = new Uri("https://sunat.test/") });

    private static HttpResponseMessage Respuesta(HttpStatusCode status, string cuerpo) =>
        new(status) { Content = new StringContent(cuerpo) };

    /// <summary>Simula la red: devuelve lo que indique la prueba, sin salir a internet.</summary>
    private sealed class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(responder(request));
        }
    }
}
