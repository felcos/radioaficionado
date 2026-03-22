using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.ObjetosDeValor;

/// <summary>
/// Tests unitarios para el enum <see cref="ModoOperacion"/> y sus extensiones.
/// Valida la clasificación digital/analógico, señal débil, obtención de modo principal
/// desde submodos y conversión desde cadena ADIF.
/// </summary>
public class ModoOperacionTests
{
    [Fact]
    public void EsDigital_FT8_DevuelveTrue()
    {
        // Arrange
        ModoOperacion modo = ModoOperacion.FT8;

        // Act
        bool esDigital = modo.EsDigital();

        // Assert
        esDigital.Should().BeTrue();
    }

    [Fact]
    public void EsDigital_SSB_DevuelveFalse()
    {
        // Arrange
        ModoOperacion modo = ModoOperacion.SSB;

        // Act
        bool esDigital = modo.EsDigital();

        // Assert
        esDigital.Should().BeFalse();
    }

    [Fact]
    public void EsDigital_CW_DevuelveTrue()
    {
        // Arrange
        ModoOperacion modo = ModoOperacion.CW;

        // Act
        bool esDigital = modo.EsDigital();

        // Assert
        esDigital.Should().BeTrue();
    }

    [Fact]
    public void EsSenalDebil_FT8_DevuelveTrue()
    {
        // Arrange
        ModoOperacion modo = ModoOperacion.FT8;

        // Act
        bool esSenalDebil = modo.EsSenalDebil();

        // Assert
        esSenalDebil.Should().BeTrue();
    }

    [Fact]
    public void EsSenalDebil_RTTY_DevuelveFalse()
    {
        // Arrange
        ModoOperacion modo = ModoOperacion.RTTY;

        // Act
        bool esSenalDebil = modo.EsSenalDebil();

        // Assert
        esSenalDebil.Should().BeFalse();
    }

    [Fact]
    public void ObtenerModoPrincipal_PSK31_DevuelvePSK()
    {
        // Arrange
        SubModoOperacion subModo = SubModoOperacion.PSK31;

        // Act
        ModoOperacion modo = subModo.ObtenerModoPrincipal();

        // Assert
        modo.Should().Be(ModoOperacion.PSK);
    }

    [Fact]
    public void ObtenerModoPrincipal_USB_DevuelveSSB()
    {
        // Arrange
        SubModoOperacion subModo = SubModoOperacion.USB;

        // Act
        ModoOperacion modo = subModo.ObtenerModoPrincipal();

        // Assert
        modo.Should().Be(ModoOperacion.SSB);
    }

    [Fact]
    public void ObtenerModoPrincipal_DMR_DevuelveDigitalvoice()
    {
        // Arrange
        SubModoOperacion subModo = SubModoOperacion.DMR;

        // Act
        ModoOperacion modo = subModo.ObtenerModoPrincipal();

        // Assert
        modo.Should().Be(ModoOperacion.DIGITALVOICE);
    }

    [Fact]
    public void ObtenerModoPrincipal_APRS_DevuelvePKT()
    {
        // Arrange
        SubModoOperacion subModo = SubModoOperacion.APRS;

        // Act
        ModoOperacion modo = subModo.ObtenerModoPrincipal();

        // Assert
        modo.Should().Be(ModoOperacion.PKT);
    }

    [Fact]
    public void IntentarDesdeAdif_FT8String_DevuelveTrue()
    {
        // Arrange
        string nombreAdif = "FT8";

        // Act
        bool resultado = ModoOperacionExtensiones.IntentarDesdeAdif(nombreAdif, out ModoOperacion modo);

        // Assert
        resultado.Should().BeTrue();
        modo.Should().Be(ModoOperacion.FT8);
    }
}
