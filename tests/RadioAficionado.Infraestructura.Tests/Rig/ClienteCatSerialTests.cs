using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig;
using System.IO.Ports;

namespace RadioAficionado.Infraestructura.Tests.Rig;

/// <summary>
/// Tests unitarios para <see cref="ClienteCatSerial"/>.
/// Verifica el estado inicial, manejo sin conexión y gestión de recursos.
/// </summary>
public class ClienteCatSerialTests
{
    [Fact]
    public void EstaConectado_Inicial_EsFalso()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

        // Act
        bool estaConectado = cliente.EstaConectado;

        // Assert
        estaConectado.Should().BeFalse();
    }

    [Fact]
    public void ModeloRadio_Inicial_EsNull()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

        // Act
        string? modelo = cliente.ModeloRadio;

        // Assert
        modelo.Should().BeNull();
    }

    [Fact]
    public void ObtenerPuertosDisponibles_RetornaLista()
    {
        // Act
        IReadOnlyList<string> puertos = ClienteCatSerial.ObtenerPuertosDisponibles();

        // Assert — puede estar vacía en CI, pero no debe ser null
        puertos.Should().NotBeNull();
    }

    [Fact]
    public async Task ConectarAsync_PuertoInexistente_LanzaExcepcion()
    {
        // Arrange
        ConfiguracionPuertoSerie configuracion = new ConfiguracionPuertoSerie
        {
            PuertoSerie = "COM999",
            Modelo = ModeloRadio.YaesuFt991
        };
        ClienteCatSerial cliente = new ClienteCatSerial(configuracion);

        // Act
        Func<Task> accion = async () => await cliente.ConectarAsync();

        // Assert
        await accion.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ConectarAsync_PuertoNulo_LanzaExcepcion()
    {
        // Arrange
        ConfiguracionPuertoSerie configuracion = new ConfiguracionPuertoSerie
        {
            PuertoSerie = "",
            Modelo = ModeloRadio.YaesuFt991
        };
        ClienteCatSerial cliente = new ClienteCatSerial(configuracion);

        // Act
        Func<Task> accion = async () => await cliente.ConectarAsync();

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DesconectarAsync_SinConectar_NoLanzaExcepcion()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

        // Act
        Func<Task> accion = async () => await cliente.DesconectarAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Dispose_SinConectar_NoLanzaExcepcion()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

        // Act
        Func<Task> accion = async () => await cliente.DisposeAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Dispose_MultiplesVeces_NoLanzaExcepcion()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

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
    public async Task CambiarFrecuenciaAsync_SinConectar_LanzaExcepcion()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();
        Frecuencia frecuencia = Frecuencia.DesdeHz(14_074_000);

        // Act
        Func<Task> accion = async () => await cliente.CambiarFrecuenciaAsync(frecuencia);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CambiarModoAsync_SinConectar_LanzaExcepcion()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

        // Act
        Func<Task> accion = async () => await cliente.CambiarModoAsync(ModoOperacion.FT8);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CambiarPttAsync_SinConectar_LanzaExcepcion()
    {
        // Arrange
        ClienteCatSerial cliente = new ClienteCatSerial();

        // Act
        Func<Task> accion = async () => await cliente.CambiarPttAsync(true);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void ConfiguracionPuertoSerie_ValoresPorDefecto_Correctos()
    {
        // Arrange
        ConfiguracionPuertoSerie configuracion = new ConfiguracionPuertoSerie();

        // Act & Assert
        configuracion.PuertoSerie.Should().Be("COM3");
        configuracion.VelocidadBaudios.Should().Be(38400);
        configuracion.BitsDeDatos.Should().Be(8);
        configuracion.Paridad.Should().Be(Parity.None);
        configuracion.BitsDeParada.Should().Be(StopBits.One);
        configuracion.RtsEnable.Should().BeTrue();
        configuracion.DtrEnable.Should().BeFalse();
        configuracion.Modelo.Should().Be(ModeloRadio.Automatico);
        configuracion.TimeoutLecturaMs.Should().Be(1000);
        configuracion.TimeoutEscrituraMs.Should().Be(1000);
        configuracion.IntervaloPollingMs.Should().Be(200);
    }
}
