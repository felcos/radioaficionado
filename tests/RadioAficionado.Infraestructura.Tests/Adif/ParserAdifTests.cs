using FluentAssertions;
using RadioAficionado.Infraestructura.Adif;

namespace RadioAficionado.Infraestructura.Tests.Adif;

/// <summary>
/// Tests unitarios para <see cref="ParserAdif"/>.
/// Valida el parseo de cadenas ADIF con distintos formatos, encabezados y casos límite.
/// </summary>
public class ParserAdifTests
{
    [Fact]
    public void Parsear_RegistroSimple_ParseaCorrectamente()
    {
        // Arrange
        string adif = "<call:5>EA4AB<band:3>20m<mode:3>FT8<freq:6>14.074<qso_date:8>20250322<time_on:4>1430<rst_sent:3>-10<rst_rcvd:3>-12<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Registros[0].Banda.Should().Be("20m");
        resultado.Registros[0].Modo.Should().Be("FT8");
        resultado.Registros[0].Frecuencia.Should().Be("14.074");
    }

    [Fact]
    public void Parsear_MultiplesRegistros_ParseaTodos()
    {
        // Arrange
        string adif = "<call:5>EA4AB<mode:3>FT8<eor><call:4>W1AW<mode:2>CW<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(2);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Registros[1].Indicativo.Should().Be("W1AW");
    }

    [Fact]
    public void Parsear_ConEncabezado_SaltaEncabezadoYParseaRegistros()
    {
        // Arrange
        string adif = "ADIF Export from WSJT-X\n<adif_ver:5>3.1.4<programid:6>WSJT-X<eoh>\n<call:5>EA4AB<mode:3>FT8<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Encabezado.Should().ContainKey("ADIF_VER");
        resultado.Encabezado["ADIF_VER"].Should().Be("3.1.4");
    }

    [Fact]
    public void Parsear_TagsCaseInsensitive_ParseaCorrectamente()
    {
        // Arrange
        string adif = "<CALL:5>EA4AB<MODE:3>FT8<EOR>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Registros[0].Modo.Should().Be("FT8");
    }

    [Fact]
    public void Parsear_ConTipoOpcional_ParseaCorrectamente()
    {
        // Arrange
        string adif = "<call:5:S>EA4AB<freq:6:N>14.074<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Registros[0].Frecuencia.Should().Be("14.074");
    }

    [Fact]
    public void Parsear_CadenaVacia_DevuelveResultadoVacio()
    {
        // Arrange & Act
        ResultadoParserAdif resultado = ParserAdif.Parsear("");

        // Assert
        resultado.Registros.Should().BeEmpty();
        resultado.Encabezado.Should().BeEmpty();
    }

    [Fact]
    public void Parsear_ConSaltosDeLinea_ParseaCorrectamente()
    {
        // Arrange
        string adif = "<call:5>EA4AB\n<mode:3>FT8\n<eor>\n";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Registros[0].Modo.Should().Be("FT8");
    }

    [Fact]
    public void Parsear_CamposPota_ParseaReferencias()
    {
        // Arrange
        string adif = "<call:5>EA4AB<pota_ref:6>K-1234<my_pota_ref:6>K-5678<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        resultado.Registros[0].ReferenciaPota.Should().Be("K-1234");
        resultado.Registros[0].MiReferenciaPota.Should().Be("K-5678");
    }

    [Fact]
    public void Parsear_FormatoWsjtx_ParseaCorrectamente()
    {
        // Arrange
        string adif = "WSJT-X ADIF Export\r\n" +
                       "<adif_ver:5>3.1.0\r\n" +
                       "<created_timestamp:15>20250322 143000\r\n" +
                       "<programid:6>WSJT-X\r\n" +
                       "<programversion:5>2.6.1\r\n" +
                       "<eoh>\r\n" +
                       "<call:5>EA4AB <gridsquare:4>IN80 <mode:3>FT8 <rst_sent:3>-10 <rst_rcvd:3>-15 " +
                       "<qso_date:8>20250322 <time_on:6>143015 <time_off:6>143115 <band:3>20m " +
                       "<freq:8>14.07400 <station_callsign:4>W1AW <my_gridsquare:6>FN31PR " +
                       "<tx_pwr:2>50 <eor>\r\n";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        RegistroAdif reg = resultado.Registros[0];
        reg.Indicativo.Should().Be("EA4AB");
        reg.Localizador.Should().Be("IN80");
        reg.Modo.Should().Be("FT8");
        reg.IndicativoPropio.Should().Be("W1AW");
        reg.Banda.Should().Be("20m");
    }

    [Fact]
    public void Parsear_SoloEncabezadoSinRegistros_DevuelveEncabezadoYRegistrosVacios()
    {
        // Arrange
        string adif = "<adif_ver:5>3.1.4<programid:6>WSJT-X<eoh>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().BeEmpty();
        resultado.Encabezado.Should().ContainKey("ADIF_VER");
        resultado.Encabezado.Should().ContainKey("PROGRAMID");
    }

    [Fact]
    public void Parsear_CamposSenalEnviadaYRecibida_ParseaCorrectamente()
    {
        // Arrange
        string adif = "<call:5>EA4AB<rst_sent:3>-10<rst_rcvd:3>-15<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros[0].SenalEnviada.Should().Be("-10");
        resultado.Registros[0].SenalRecibida.Should().Be("-15");
    }

    [Fact]
    public void Parsear_CamposPotenciaYLocalizador_ParseaCorrectamente()
    {
        // Arrange
        string adif = "<call:5>EA4AB<tx_pwr:3>100<gridsquare:6>IN80AB<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros[0].Potencia.Should().Be("100");
        resultado.Registros[0].Localizador.Should().Be("IN80AB");
    }

    [Fact]
    public void Parsear_TotalRegistros_DevuelveCantidadCorrecta()
    {
        // Arrange
        string adif = "<call:5>EA4AB<eor><call:4>W1AW<eor><call:6>VK2ABC<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.TotalRegistros.Should().Be(3);
    }

    [Fact]
    public void Parsear_ContenidoNulo_LanzaArgumentNullException()
    {
        // Arrange & Act
        Action accion = () => ParserAdif.Parsear(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parsear_RegistroConFechaYHora_ParseaFormatoCorrecto()
    {
        // Arrange
        string adif = "<call:5>EA4AB<qso_date:8>20250315<time_on:6>143015<time_off:6>143115<eor>";

        // Act
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros[0].FechaQso.Should().Be("20250315");
        resultado.Registros[0].HoraInicio.Should().Be("143015");
        resultado.Registros[0].HoraFin.Should().Be("143115");
    }
}
