using System.Text;
using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig.Cat;

namespace RadioAficionado.Infraestructura.Tests.Rig;

/// <summary>
/// Tests unitarios para <see cref="ProtocoloYaesu"/>.
/// Verifica la generación y parseo de comandos CAT Yaesu.
/// </summary>
public class ProtocoloYaesuTests
{
    private readonly ProtocoloYaesu _protocolo = new ProtocoloYaesu();

    [Fact]
    public void ComandoLeerFrecuencia_RetornaFaSemicolon()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerFrecuencia();

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FA;");
    }

    [Fact]
    public void ComandoCambiarFrecuencia_14Mhz_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarFrecuencia(14_074_000);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FA00014074000;");
    }

    [Fact]
    public void ParsearFrecuencia_Respuesta14074_RetornaHz()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("FA00014074000;");

        // Act
        long frecuenciaHz = _protocolo.ParsearFrecuencia(respuesta);

        // Assert
        frecuenciaHz.Should().Be(14_074_000);
    }

    [Fact]
    public void ParsearFrecuencia_Respuesta7074_RetornaHz()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("FA00007074000;");

        // Act
        long frecuenciaHz = _protocolo.ParsearFrecuencia(respuesta);

        // Assert
        frecuenciaHz.Should().Be(7_074_000);
    }

    [Fact]
    public void ComandoLeerModo_RetornaMd0Semicolon()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerModo();

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("MD0;");
    }

    [Fact]
    public void ComandoCambiarModo_Usb_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarModo(ModoOperacion.SSB);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("MD02;");
    }

    [Fact]
    public void ComandoCambiarModo_Ft8_UsaDataUsb()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarModo(ModoOperacion.FT8);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("MD0C;");
    }

    [Fact]
    public void ParsearModo_Usb_RetornaSsbUsb()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("MD02;");

        // Act
        (ModoOperacion modo, SubModoOperacion? submodo) = _protocolo.ParsearModo(respuesta);

        // Assert
        modo.Should().Be(ModoOperacion.SSB);
        submodo.Should().Be(SubModoOperacion.USB);
    }

    [Fact]
    public void ParsearModo_DataUsb_RetornaFt8()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("MD0C;");

        // Act
        (ModoOperacion modo, SubModoOperacion? submodo) = _protocolo.ParsearModo(respuesta);

        // Assert
        modo.Should().Be(ModoOperacion.FT8);
    }

    [Fact]
    public void ComandoPtt_Activar_RetornaTx1()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarPtt(true);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("TX1;");
    }

    [Fact]
    public void ComandoActivarSplit_Activar_RetornaFt1()
    {
        // Act
        byte[] comando = _protocolo.ComandoActivarSplit(true);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FT1;");
    }

    [Fact]
    public void ComandoActivarSplit_Desactivar_RetornaFt0()
    {
        // Act
        byte[] comando = _protocolo.ComandoActivarSplit(false);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FT0;");
    }

    [Fact]
    public void ComandoLeerSplit_RetornaFtSemicolon()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerSplit();

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FT;");
    }

    [Fact]
    public void ParsearSplit_Activo_RetornaTrue()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("FT1;");

        // Act
        bool splitActivo = _protocolo.ParsearSplit(respuesta);

        // Assert
        splitActivo.Should().BeTrue();
    }

    [Fact]
    public void ParsearSplit_Inactivo_RetornaFalse()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("FT0;");

        // Act
        bool splitActivo = _protocolo.ParsearSplit(respuesta);

        // Assert
        splitActivo.Should().BeFalse();
    }

    [Fact]
    public void ComandoCambiarFrecuenciaVfoB_14Mhz_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarFrecuenciaVfoB(14_074_000);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FB00014074000;");
    }
}
