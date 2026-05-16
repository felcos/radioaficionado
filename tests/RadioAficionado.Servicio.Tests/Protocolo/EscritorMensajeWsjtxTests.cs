using FluentAssertions;
using RadioAficionado.Servicio.Protocolo;

namespace RadioAficionado.Servicio.Tests.Protocolo;

/// <summary>
/// Tests para EscritorMensajeWsjtx — serialización binaria del protocolo WSJT-X.
/// </summary>
public sealed class EscritorMensajeWsjtxTests
{
    [Fact]
    public void SerializarHeartbeat_ContieneHeaderCorrecto()
    {
        // Arrange
        MensajeHeartbeat heartbeat = new("RadioAficionado", 2, "1.0.0", "");

        // Act
        byte[] bytes = EscritorMensajeWsjtx.SerializarHeartbeat(heartbeat);

        // Assert
        bytes.Should().NotBeEmpty();
        // Verificar magic number (big-endian): 0xADBCCBDA
        bytes[0].Should().Be(0xAD);
        bytes[1].Should().Be(0xBC);
        bytes[2].Should().Be(0xCB);
        bytes[3].Should().Be(0xDA);
        // Schema version = 2
        bytes[4].Should().Be(0x00);
        bytes[5].Should().Be(0x00);
        bytes[6].Should().Be(0x00);
        bytes[7].Should().Be(0x02);
        // Tipo = 0 (Heartbeat)
        bytes[8].Should().Be(0x00);
        bytes[9].Should().Be(0x00);
        bytes[10].Should().Be(0x00);
        bytes[11].Should().Be(0x00);
    }

    [Fact]
    public void SerializarDecode_ContieneHeaderYDatos()
    {
        // Arrange
        MensajeDecode decode = new(
            "RadioAficionado", true, 45000, -15, 0.1, 1500,
            "FT8", "CQ EA1ABC JM28", false, false);

        // Act
        byte[] bytes = EscritorMensajeWsjtx.SerializarDecode(decode);

        // Assert
        bytes.Should().NotBeEmpty();
        // Magic
        bytes[0].Should().Be(0xAD);
        bytes[1].Should().Be(0xBC);
        // Tipo = 2 (Decode)
        bytes[11].Should().Be(0x02);
    }

    [Fact]
    public void EscribirString_StringVacio_EscribeLongitudCero()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();

        // Act
        escritor.EscribirString("");
        byte[] bytes = escritor.ObtenerBytes();

        // Assert
        bytes.Should().HaveCount(4); // Solo uint32 longitud = 0
        bytes[0].Should().Be(0);
        bytes[1].Should().Be(0);
        bytes[2].Should().Be(0);
        bytes[3].Should().Be(0);
    }

    [Fact]
    public void EscribirString_Null_EscribeFFFFFFFF()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();

        // Act
        escritor.EscribirString(null);
        byte[] bytes = escritor.ObtenerBytes();

        // Assert
        bytes.Should().HaveCount(4);
        bytes[0].Should().Be(0xFF);
        bytes[1].Should().Be(0xFF);
        bytes[2].Should().Be(0xFF);
        bytes[3].Should().Be(0xFF);
    }

    [Fact]
    public void EscribirString_TextoCorto_EscribeLongitudYUtf8()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();

        // Act
        escritor.EscribirString("CQ");
        byte[] bytes = escritor.ObtenerBytes();

        // Assert
        bytes.Should().HaveCount(6); // 4 bytes longitud + 2 bytes "CQ"
        bytes[3].Should().Be(2); // Longitud = 2
        bytes[4].Should().Be((byte)'C');
        bytes[5].Should().Be((byte)'Q');
    }

    [Fact]
    public void SerializarStatus_ContieneHeaderYFrecuencia()
    {
        // Arrange
        MensajeStatus status = new(
            "RadioAficionado", 14074000, "FT8", "", "", "FT8",
            true, false, false, 1500, 1500, "EA1ABC", "JM28", "",
            false, "", false, 0, 0, 15, "Default");

        // Act
        byte[] bytes = EscritorMensajeWsjtx.SerializarStatus(status);

        // Assert
        bytes.Should().NotBeEmpty();
        bytes[11].Should().Be(0x01); // Tipo = 1 (Status)
    }

    [Fact]
    public void EscribirUInt32_BigEndian_OrdenCorrecto()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();

        // Act
        escritor.EscribirUInt32(0x12345678);
        byte[] bytes = escritor.ObtenerBytes();

        // Assert
        bytes[0].Should().Be(0x12);
        bytes[1].Should().Be(0x34);
        bytes[2].Should().Be(0x56);
        bytes[3].Should().Be(0x78);
    }

    [Fact]
    public void SerializarAdif_ContieneHeaderYTextoAdif()
    {
        // Arrange
        string adif = "<call:5>W1AW <mode:3>FT8 <eor>";

        // Act
        byte[] bytes = EscritorMensajeWsjtx.SerializarAdif("RadioAficionado", adif);

        // Assert
        bytes.Should().NotBeEmpty();
        bytes[11].Should().Be(12); // Tipo = 12 (LoggedADIF)
    }
}
