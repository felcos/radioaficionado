using FluentAssertions;
using RadioAficionado.Compartido.Contratos;

namespace RadioAficionado.Dominio.Tests.Contratos;

/// <summary>
/// Tests unitarios para el record <see cref="ComandoRemotoRig"/>.
/// </summary>
public class ComandoRemotoRigTests
{
    [Fact]
    public void Crear_ConTipoYUsuario_GeneraIdYFecha()
    {
        // Arrange
        TipoComandoRig tipo = TipoComandoRig.CambiarFrecuencia;
        string usuarioId = "usuario-test-001";

        // Act
        ComandoRemotoRig comando = ComandoRemotoRig.Crear(tipo, usuarioId);

        // Assert
        comando.Id.Should().NotBeEmpty();
        comando.Tipo.Should().Be(TipoComandoRig.CambiarFrecuencia);
        comando.UsuarioId.Should().Be("usuario-test-001");
        comando.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_SinPayload_PayloadVacio()
    {
        // Arrange
        TipoComandoRig tipo = TipoComandoRig.ObtenerEstado;
        string usuarioId = "usuario-test-002";

        // Act
        ComandoRemotoRig comando = ComandoRemotoRig.Crear(tipo, usuarioId);

        // Assert
        comando.Payload.Should().NotBeNull();
        comando.Payload.Should().BeEmpty();
    }

    [Fact]
    public void Crear_ConPayload_ContieneValores()
    {
        // Arrange
        TipoComandoRig tipo = TipoComandoRig.CambiarFrecuencia;
        string usuarioId = "usuario-test-003";
        Dictionary<string, string> payload = new()
        {
            { "frecuencia", "14074000" },
            { "modo", "FT8" }
        };

        // Act
        ComandoRemotoRig comando = ComandoRemotoRig.Crear(tipo, usuarioId, payload);

        // Assert
        comando.Payload.Should().HaveCount(2);
        comando.Payload.Should().ContainKey("frecuencia").WhoseValue.Should().Be("14074000");
        comando.Payload.Should().ContainKey("modo").WhoseValue.Should().Be("FT8");
    }

    [Fact]
    public void Record_Igualdad_MismosDatos()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        DateTime fecha = DateTime.UtcNow;
        IReadOnlyDictionary<string, string> payload = new Dictionary<string, string>
        {
            { "clave", "valor" }
        };

        ComandoRemotoRig comando1 = new(
            id,
            TipoComandoRig.CambiarModo,
            "usuario-test-004",
            payload,
            fecha);

        ComandoRemotoRig comando2 = new(
            id,
            TipoComandoRig.CambiarModo,
            "usuario-test-004",
            payload,
            fecha);

        // Act & Assert
        comando1.Should().Be(comando2);
    }
}
