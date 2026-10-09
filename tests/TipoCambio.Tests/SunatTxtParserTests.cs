using System.Globalization;
using TipoCambio.Core.Sunat;

namespace TipoCambio.Tests;

public class SunatTxtParserTests
{
    [Theory]
    [InlineData("09/10/2026|3.446|3.453|")]
    [InlineData("09/10/2026|3.446|3.453|\n")]
    [InlineData("  09/10/2026|3.446|3.453|\r\n")]
    [InlineData("09/10/2026|3.446|3.453")]
    public void Parse_FormatoValido_DevuelveTipoCambio(string contenido)
    {
        var resultado = SunatTxtParser.Parse(contenido);

        Assert.Equal(new DateOnly(2026, 10, 9), resultado.Fecha);
        Assert.Equal(3.446m, resultado.Compra);
        Assert.Equal(3.453m, resultado.Venta);
        Assert.Equal("SUNAT", resultado.Fuente);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>Servicio no disponible</html>")]
    [InlineData("09/10/2026|3.446|")]
    [InlineData("09/10/2026|3.446|3.453|9.999|")]
    [InlineData("2026-10-09|3.446|3.453|")]
    [InlineData("31/02/2026|3.446|3.453|")]
    [InlineData("09/10/2026|abc|3.453|")]
    [InlineData("09/10/2026|3,446|3.453|")]
    [InlineData("09/10/2026|0|3.453|")]
    [InlineData("09/10/2026|-3.446|3.453|")]
    public void Parse_FormatoInvalido_LanzaFormatException(string contenido)
    {
        Assert.Throws<FormatException>(() => SunatTxtParser.Parse(contenido));
    }

    [Fact]
    public void Parse_NoDependeDeLaCulturaDelSistema()
    {
        var culturaOriginal = CultureInfo.CurrentCulture;
        try
        {
            // es-PE usa coma decimal en algunos sistemas; el resultado no debe cambiar.
            CultureInfo.CurrentCulture = new CultureInfo("es-PE");

            var resultado = SunatTxtParser.Parse("09/10/2026|3.446|3.453|");

            Assert.Equal(3.446m, resultado.Compra);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaOriginal;
        }
    }
}
