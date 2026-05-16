using System.Text;
using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig.Cat;

namespace RadioAficionado.Infraestructura.Tests.Rig;

/// <summary>
/// Tests unitarios para <see cref="ProtocoloKenwood"/>.
/// Verifica la generación y parseo de comandos CAT de Kenwood/Elecraft.
/// </summary>
public class ProtocoloKenwoodTests
{
    private readonly ProtocoloKenwood _protocolo = new ProtocoloKenwood();

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
    public void ParsearFrecuencia_RetornaHz()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("FA00014074000;");

        // Act
        long frecuenciaHz = _protocolo.ParsearFrecuencia(respuesta);

        // Assert
        frecuenciaHz.Should().Be(14_074_000);
    }

    [Fact]
    public void ComandoCambiarModo_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarModo(ModoOperacion.CW);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("MD3;");
    }

    [Fact]
    public void ParsearModo_RetornaCorrecto()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("MD2;");

        // Act
        (ModoOperacion modo, SubModoOperacion? submodo) = _protocolo.ParsearModo(respuesta);

        // Assert
        modo.Should().Be(ModoOperacion.SSB);
        submodo.Should().Be(SubModoOperacion.USB);
    }

    [Fact]
    public void ComandoPtt_Tx_RetornaTxSemicolon()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarPtt(true);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("TX;");
    }

    [Fact]
    public void ParsearNivelSenal_EnRango()
    {
        // Arrange
        byte[] respuesta = Encoding.ASCII.GetBytes("SM0025;");

        // Act
        int nivel = _protocolo.ParsearNivelSenal(respuesta);

        // Assert
        nivel.Should().BeInRange(0, 30);
    }

    [Fact]
    public void ComandoCambiarFrecuencia_FormatoOncedigitos()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarFrecuencia(7_074_000);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FA00007074000;");
        texto.Should().HaveLength(14); // FA + 11 dígitos + ;
    }

    [Fact]
    public void NombreFabricante_EsKenwood()
    {
        // Act
        string fabricante = _protocolo.NombreFabricante;

        // Assert
        fabricante.Should().Be("Kenwood");
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
    public void ComandoCambiarFrecuenciaVfoB_7Mhz_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarFrecuenciaVfoB(7_074_000);

        // Assert
        string texto = Encoding.ASCII.GetString(comando);
        texto.Should().Be("FB00007074000;");
    }
}
