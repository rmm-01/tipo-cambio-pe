using TipoCambio.Core.Bcrp;

namespace TipoCambio.Tests;

public class BcrpJsonParserTests
{
    // Recorte de una respuesta real de la API (octubre de 2026).
    private const string RespuestaReal = """
        {"config":{"title":"Tipo de cambio","series":[{"name":"Compra","dec":"3"},{"name":"Venta","dec":"3"}]},
         "periods":[
           {"name":"02.Oct.26","values":["3.437","3.442"]},
           {"name":"05.Oct.26","values":["3.423","3.435"]},
           {"name":"06.Oct.26","values":["3.431","3.437"]},
           {"name":"07.Oct.26","values":["n.d.","n.d."]},
           {"name":"08.Oct.26","values":["n.d.","n.d."]}
         ]}
        """;

    [Fact]
    public void Parse_OmiteDiasSinDato_YDevuelveEnOrden()
    {
        var dias = BcrpJsonParser.Parse(RespuestaReal);

        Assert.Equal(3, dias.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), dias[^1].Fecha);
        Assert.Equal(3.431m, dias[^1].Compra);
        Assert.Equal(3.437m, dias[^1].Venta);
        Assert.All(dias, d => Assert.Equal("BCRP", d.Fuente));
    }

    [Fact]
    public void Parse_TodosSinDato_DevuelveListaVacia()
    {
        var dias = BcrpJsonParser.Parse("""{"periods":[{"name":"07.Oct.26","values":["n.d.","n.d."]}]}""");

        Assert.Empty(dias);
    }

    [Theory]
    [InlineData("01.Ene.26", 1)]
    [InlineData("15.Ago.26", 8)]
    [InlineData("15.Set.26", 9)]
    [InlineData("15.Sep.26", 9)]
    [InlineData("31.Dic.26", 12)]
    public void Parse_MesesEnEspanol(string nombre, int mesEsperado)
    {
        var dias = BcrpJsonParser.Parse($$"""{"periods":[{"name":"{{nombre}}","values":["3.4","3.5"]}]}""");

        Assert.Equal(mesEsperado, dias[0].Fecha.Month);
    }

    [Theory]
    [InlineData("<html>Error</html>")]
    [InlineData("""{"config":{}}""")]
    [InlineData("""{"periods":[{"name":"06.Oct.26","values":["3.431"]}]}""")]
    [InlineData("""{"periods":[{"name":"06/10/2026","values":["3.431","3.437"]}]}""")]
    [InlineData("""{"periods":[{"name":"06.Xyz.26","values":["3.431","3.437"]}]}""")]
    [InlineData("""{"periods":[{"name":"31.Feb.26","values":["3.431","3.437"]}]}""")]
    public void Parse_FormatoInvalido_LanzaFormatException(string json)
    {
        Assert.Throws<FormatException>(() => BcrpJsonParser.Parse(json));
    }
}
