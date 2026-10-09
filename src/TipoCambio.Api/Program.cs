using TipoCambio.Core;
using TipoCambio.Core.Sunat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddHttpClient<ITipoCambioProveedor, SunatProveedor>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Sunat:BaseUrl"] ?? "https://www.sunat.gob.pe/");
    http.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/tipo-cambio/hoy", async (ITipoCambioProveedor proveedor, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await proveedor.ObtenerHoyAsync(ct));
    }
    catch (ProveedorNoDisponibleException ex)
    {
        // 503 y no 500: el fallo es de la fuente externa, no de esta API.
        return Results.Problem(
            title: "Fuente de tipo de cambio no disponible",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("ObtenerTipoCambioHoy");

app.Run();
