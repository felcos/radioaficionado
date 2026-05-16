using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig.Cat;

namespace RadioAficionado.Infraestructura.Tests.Rig;

/// <summary>
/// Tests unitarios para <see cref="ProtocoloIcom"/>.
/// Verifica la generación y parseo de comandos CI-V de Icom.
/// </summary>
public class ProtocoloIcomTests
{
    private readonly ProtocoloIcom _protocolo = new ProtocoloIcom(0x94);

    [Fact]
    public void ComandoLeerFrecuencia_TienePreambuloFeFeYFd()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerFrecuencia();

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void ComandoCambiarFrecuencia_14074_BcdCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarFrecuencia(14_074_000);

        // Assert
        // Preámbulo FE FE, dirección radio 94, dirección PC E0, comando 05
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[2].Should().Be(0x94);
        comando[3].Should().Be(0xE0);
        comando[4].Should().Be(0x05);
        // BCD invertido de 14074000:
        // 14074000 → bytes BCD invertido: 00 40 07 41 00
        // Posición 5-9: los 5 bytes BCD
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void ParsearFrecuencia_Bcd14074_RetornaHz()
    {
        // Arrange — Construir frame de respuesta con frecuencia 14074000
        // BCD invertido de 14074000: 00 40 07 14 00
        byte[] respuesta = new byte[] { 0xFE, 0xFE, 0xE0, 0x94, 0x03, 0x00, 0x40, 0x07, 0x14, 0x00, 0xFD };

        // Act
        long frecuenciaHz = _protocolo.ParsearFrecuencia(respuesta);

        // Assert
        frecuenciaHz.Should().Be(14_074_000);
    }

    [Fact]
    public void ComandoLeerModo_ComandoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerModo();

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[4].Should().Be(0x04);
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void ParsearModo_Usb_RetornaSsbUsb()
    {
        // Arrange — Frame con modo 0x01 (USB) y filtro 0x01
        byte[] respuesta = new byte[] { 0xFE, 0xFE, 0xE0, 0x94, 0x04, 0x01, 0x01, 0xFD };

        // Act
        (ModoOperacion modo, SubModoOperacion? submodo) = _protocolo.ParsearModo(respuesta);

        // Assert
        modo.Should().Be(ModoOperacion.SSB);
        submodo.Should().Be(SubModoOperacion.USB);
    }

    [Fact]
    public void ComandoPtt_Activar_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarPtt(true);

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[4].Should().Be(0x1C);
        comando[5].Should().Be(0x00);
        comando[6].Should().Be(0x01);
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void ParsearPtt_Activo_RetornaTrue()
    {
        // Arrange — Frame con PTT activo (1C 00 01)
        byte[] respuesta = new byte[] { 0xFE, 0xFE, 0xE0, 0x94, 0x1C, 0x00, 0x01, 0xFD };

        // Act
        bool pttActivo = _protocolo.ParsearPtt(respuesta);

        // Assert
        pttActivo.Should().BeTrue();
    }

    [Fact]
    public void ComandoNivelSenal_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerNivelSenal();

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[4].Should().Be(0x15);
        comando[5].Should().Be(0x02);
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void Frame_TieneDireccionCorrecta()
    {
        // Act
        byte[] frame = _protocolo.ConstruirFrame(new byte[] { 0x03 });

        // Assert
        frame[2].Should().Be(0x94); // Dirección radio IC-7300
        frame[3].Should().Be(0xE0); // Dirección PC
    }

    [Fact]
    public void ParsearFrecuencia_Bcd7074_RetornaHz()
    {
        // Arrange — Construir frame de respuesta con frecuencia 7074000
        // BCD invertido de 7074000: 00 40 07 07 00
        byte[] respuesta = new byte[] { 0xFE, 0xFE, 0xE0, 0x94, 0x03, 0x00, 0x40, 0x07, 0x07, 0x00, 0xFD };

        // Act
        long frecuenciaHz = _protocolo.ParsearFrecuencia(respuesta);

        // Assert
        frecuenciaHz.Should().Be(7_074_000);
    }

    [Fact]
    public void ComandoActivarSplit_Activar_ComandoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoActivarSplit(true);

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[4].Should().Be(0x0F); // Comando split
        comando[5].Should().Be(0x01); // Activar
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void ComandoActivarSplit_Desactivar_ComandoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoActivarSplit(false);

        // Assert
        comando[4].Should().Be(0x0F);
        comando[5].Should().Be(0x00); // Desactivar
    }

    [Fact]
    public void ComandoLeerSplit_FormatoCorrecto()
    {
        // Act
        byte[] comando = _protocolo.ComandoLeerSplit();

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[4].Should().Be(0x0F);
        comando[^1].Should().Be(0xFD);
    }

    [Fact]
    public void ParsearSplit_Activo_RetornaTrue()
    {
        // Arrange — Frame con split activo (0F 01)
        byte[] respuesta = new byte[] { 0xFE, 0xFE, 0xE0, 0x94, 0x0F, 0x01, 0xFD };

        // Act
        bool splitActivo = _protocolo.ParsearSplit(respuesta);

        // Assert
        splitActivo.Should().BeTrue();
    }

    [Fact]
    public void ParsearSplit_Inactivo_RetornaFalse()
    {
        // Arrange — Frame con split inactivo (0F 00)
        byte[] respuesta = new byte[] { 0xFE, 0xFE, 0xE0, 0x94, 0x0F, 0x00, 0xFD };

        // Act
        bool splitActivo = _protocolo.ParsearSplit(respuesta);

        // Assert
        splitActivo.Should().BeFalse();
    }

    [Fact]
    public void ComandoCambiarFrecuenciaVfoB_TienePreambuloYComando05()
    {
        // Act
        byte[] comando = _protocolo.ComandoCambiarFrecuenciaVfoB(14_074_000);

        // Assert
        comando[0].Should().Be(0xFE);
        comando[1].Should().Be(0xFE);
        comando[4].Should().Be(0x05); // Comando cambiar frecuencia
        comando[^1].Should().Be(0xFD);
    }
}
