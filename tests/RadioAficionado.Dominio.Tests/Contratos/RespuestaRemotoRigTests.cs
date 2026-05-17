using FluentAssertions;
using RadioAficionado.Compartido.Contratos;

namespace RadioAficionado.Dominio.Tests.Contratos;

/// <summary>
/// Tests unitarios para el record <see cref="RespuestaRemotoRig"/>.
/// </summary>
public class RespuestaRemotoRigTests
{
    [Fact]
    public void Exito_SinDatos_ExitosoTrue()
    {
        // Arrange
        Guid comandoId = Guid.NewGuid();

        // Act
        RespuestaRemotoRig respuesta = RespuestaRemotoRig.Exito(comandoId);

        // Assert
        respuesta.ComandoId.Should().Be(comandoId);
        respuesta.Exitoso.Should().BeTrue();
        respuesta.MensajeError.Should().BeNull();
        respuesta.Datos.Should().BeNull();
        respuesta.FechaRespuesta.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Exito_ConDatos_ContieneValores()
    {
        // Arrange
        Guid comandoId = Guid.NewGuid();
        Dictionary<string, string> datos = new()
        {
            { "frecuencia", "7074000" },
            { "modo", "FT8" },
            { "banda", "40m" }
        };

        // Act
        RespuestaRemotoRig respuesta = RespuestaRemotoRig.Exito(comandoId, datos);

        // Assert
        respuesta.ComandoId.Should().Be(comandoId);
        respuesta.Exitoso.Should().BeTrue();
        respuesta.MensajeError.Should().BeNull();
        respuesta.Datos.Should().NotBeNull();
        respuesta.Datos.Should().HaveCount(3);
        respuesta.Datos!.Should().ContainKey("frecuencia").WhoseValue.Should().Be("7074000");
        respuesta.Datos!.Should().ContainKey("banda").WhoseValue.Should().Be("40m");
    }

    [Fact]
    public void Error_ConMensaje_ExitosoFalse()
    {
        // Arrange
        Guid comandoId = Guid.NewGuid();
        string mensajeError = "No se pudo conectar al rig";

        // Act
        RespuestaRemotoRig respuesta = RespuestaRemotoRig.Error(comandoId, mensajeError);

        // Assert
        respuesta.ComandoId.Should().Be(comandoId);
        respuesta.Exitoso.Should().BeFalse();
        respuesta.Datos.Should().BeNull();
        respuesta.FechaRespuesta.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Error_ConMensaje_MensajeErrorCorrecto()
    {
        // Arrange
        Guid comandoId = Guid.NewGuid();
        string mensajeError = "Timeout al enviar comando CAT";

        // Act
        RespuestaRemotoRig respuesta = RespuestaRemotoRig.Error(comandoId, mensajeError);

        // Assert
        respuesta.MensajeError.Should().Be("Timeout al enviar comando CAT");
    }
}
