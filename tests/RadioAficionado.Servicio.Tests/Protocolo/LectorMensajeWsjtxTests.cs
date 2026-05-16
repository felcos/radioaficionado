using FluentAssertions;
using RadioAficionado.Servicio.Protocolo;

namespace RadioAficionado.Servicio.Tests.Protocolo;

/// <summary>
/// Tests para LectorMensajeWsjtx — deserializacion binaria del protocolo WSJT-X.
/// </summary>
public sealed class LectorMensajeWsjtxTests
{
    [Fact]
    public void LeerHeader_MagicValido_RetornaTipo()
    {
        // Arrange — Heartbeat serializado
        byte[] bytes = EscritorMensajeWsjtx.SerializarHeartbeat(
            new MensajeHeartbeat("test", 2, "1.0", ""));

        LectorMensajeWsjtx lector = new(bytes);

        // Act
        TipoMensajeWsjtx? tipo = lector.LeerHeader();

        // Assert
        tipo.Should().NotBeNull();
        tipo!.Value.Should().Be(TipoMensajeWsjtx.Heartbeat);
    }

    [Fact]
    public void LeerHeader_MagicInvalido_RetornaNull()
    {
        // Arrange
        byte[] bytes = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00];
        LectorMensajeWsjtx lector = new(bytes);

        // Act
        TipoMensajeWsjtx? tipo = lector.LeerHeader();

        // Assert
        tipo.Should().BeNull();
    }

    [Fact]
    public void LeerHeader_DatosMuyCortos_RetornaNull()
    {
        // Arrange
        byte[] bytes = [0xAD, 0xBC];
        LectorMensajeWsjtx lector = new(bytes);

        // Act
        TipoMensajeWsjtx? tipo = lector.LeerHeader();

        // Assert
        tipo.Should().BeNull();
    }

    [Fact]
    public void LeerString_Roundtrip_Preserva()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirString("CQ EA1ABC JM28");
        byte[] bytes = escritor.ObtenerBytes();

        LectorMensajeWsjtx lector = new(bytes);

        // Act
        string? resultado = lector.LeerString();

        // Assert
        resultado.Should().Be("CQ EA1ABC JM28");
    }

    [Fact]
    public void LeerString_Null_RetornaNull()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirString(null);
        byte[] bytes = escritor.ObtenerBytes();

        LectorMensajeWsjtx lector = new(bytes);

        // Act
        string? resultado = lector.LeerString();

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void LeerUInt32_Roundtrip_Preserva()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirUInt32(42);
        byte[] bytes = escritor.ObtenerBytes();

        LectorMensajeWsjtx lector = new(bytes);

        // Act
        uint resultado = lector.LeerUInt32();

        // Assert
        resultado.Should().Be(42);
    }

    [Fact]
    public void LeerInt32_ValorNegativo_Roundtrip()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirInt32(-15);
        byte[] bytes = escritor.ObtenerBytes();

        LectorMensajeWsjtx lector = new(bytes);

        // Act
        int resultado = lector.LeerInt32();

        // Assert
        resultado.Should().Be(-15);
    }

    [Fact]
    public void LeerDouble_Roundtrip_Preserva()
    {
        // Arrange
        EscritorMensajeWsjtx escritor = new();
        escritor.EscribirDouble(3.14159);
        byte[] bytes = escritor.ObtenerBytes();

        LectorMensajeWsjtx lector = new(bytes);

        // Act
        double resultado = lector.LeerDouble();

        // Assert
        resultado.Should().BeApproximately(3.14159, 0.0001);
    }

    [Fact]
    public void Decode_Roundtrip_Completo()
    {
        // Arrange
        MensajeDecode original = new(
            "RadioAficionado", true, 45000, -15, 0.1, 1500,
            "FT8", "CQ EA1ABC JM28", false, false);

        byte[] bytes = EscritorMensajeWsjtx.SerializarDecode(original);
        LectorMensajeWsjtx lector = new(bytes);

        // Act
        TipoMensajeWsjtx? tipo = lector.LeerHeader();
        string? id = lector.LeerString();
        bool esNuevo = lector.LeerBool();
        uint timeMs = lector.LeerUInt32();
        int snr = lector.LeerInt32();
        double deltaTime = lector.LeerDouble();
        uint deltaFreq = lector.LeerUInt32();
        string? modo = lector.LeerString();
        string? mensaje = lector.LeerString();

        // Assert
        tipo.Should().Be(TipoMensajeWsjtx.Decode);
        id.Should().Be("RadioAficionado");
        esNuevo.Should().BeTrue();
        timeMs.Should().Be(45000);
        snr.Should().Be(-15);
        deltaTime.Should().BeApproximately(0.1, 0.001);
        deltaFreq.Should().Be(1500);
        modo.Should().Be("FT8");
        mensaje.Should().Be("CQ EA1ABC JM28");
    }
}
