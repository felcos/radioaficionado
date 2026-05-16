using FluentAssertions;
using RadioAficionado.Nativo.Rig;

namespace RadioAficionado.Infraestructura.Tests.Rig;

/// <summary>
/// Tests unitarios para <see cref="ClienteRigctld"/>.
/// Verifica el estado inicial, manejo sin conexión y gestión de recursos.
/// </summary>
public class ClienteRigctldTests
{
    [Fact]
    public void EstaConectado_Inicial_EsFalso()
    {
        // Arrange
        ClienteRigctld cliente = new ClienteRigctld();

        // Act
        bool estaConectado = cliente.EstaConectado;

        // Assert
        estaConectado.Should().BeFalse();
    }

    [Fact]
    public void ModeloRadio_Inicial_EsNull()
    {
        // Arrange
        ClienteRigctld cliente = new ClienteRigctld();

        // Act
        string? modelo = cliente.ModeloRadio;

        // Assert
        modelo.Should().BeNull();
    }

    [Fact]
    public async Task ConectarAsync_ServidorNoDisponible_LanzaExcepcion()
    {
        // Arrange
        ClienteRigctld cliente = new ClienteRigctld();

        // Act
        Func<Task> accion = async () => await cliente.ConectarAsync("localhost", 59998);

        // Assert
        await accion.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DesconectarAsync_SinConectar_NoLanzaExcepcion()
    {
        // Arrange
        ClienteRigctld cliente = new ClienteRigctld();

        // Act
        Func<Task> accion = async () => await cliente.DesconectarAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CambiarFrecuenciaAsync_SinConectar_LanzaInvalidOperationException()
    {
        // Arrange
        ClienteRigctld cliente = new ClienteRigctld();
        RadioAficionado.Dominio.ObjetosDeValor.Frecuencia frecuencia =
            RadioAficionado.Dominio.ObjetosDeValor.Frecuencia.DesdeHz(14_074_000);

        // Act
        Func<Task> accion = async () => await cliente.CambiarFrecuenciaAsync(frecuencia);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DisposeAsync_MultiplesVeces_NoLanzaExcepcion()
    {
        // Arrange
        ClienteRigctld cliente = new ClienteRigctld();

        // Act
        Func<Task> accion = async () =>
        {
            await cliente.DisposeAsync();
            await cliente.DisposeAsync();
        };

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_ConConfiguracionNula_LanzaArgumentNullException()
    {
        // Arrange & Act
        Action accion = () => new ClienteRigctld(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Configuracion_PorDefecto_EsCorrecta()
    {
        // Arrange
        ConfiguracionRig configuracion = new ConfiguracionRig();

        // Act & Assert
        configuracion.Host.Should().Be("localhost");
        configuracion.Puerto.Should().Be(4532);
        configuracion.IntervaloPollingMs.Should().Be(500);
        configuracion.PotenciaMaximaVatios.Should().Be(100.0);
        configuracion.TimeoutMs.Should().Be(5000);
    }
}
