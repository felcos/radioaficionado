using System.Reflection;
using FluentAssertions;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Tests.Servicios;

public class MetricasConexionTests
{
    [Fact]
    public void RegistrarServicioConectado_AlRegistrar_IncrementaContador()
    {
        // Arrange
        MetricasConexion metricas = new();

        // Act
        metricas.RegistrarServicioConectado();

        // Assert
        metricas.ServiciosConectados.Should().Be(1);
    }

    [Fact]
    public void RegistrarServicioDesconectado_AlDesregistrar_DecrementaContador()
    {
        // Arrange
        MetricasConexion metricas = new();
        metricas.RegistrarServicioConectado();
        metricas.RegistrarServicioConectado();

        // Act
        metricas.RegistrarServicioDesconectado();

        // Assert
        metricas.ServiciosConectados.Should().Be(1);
    }

    [Fact]
    public void RegistrarBrowserConectado_AlRegistrar_IncrementaContador()
    {
        // Arrange
        MetricasConexion metricas = new();

        // Act
        metricas.RegistrarBrowserConectado();

        // Assert
        metricas.BrowsersConectados.Should().Be(1);
    }

    [Fact]
    public void RegistrarComandoEjecutado_MultiplesComandos_ContadorCorrecto()
    {
        // Arrange
        MetricasConexion metricas = new();

        // Act
        metricas.RegistrarComandoEjecutado();
        metricas.RegistrarComandoEjecutado();
        metricas.RegistrarComandoEjecutado();

        // Assert
        metricas.ComandosEjecutados.Should().Be(3);
    }

    [Fact]
    public void RegistrarError_AlRegistrar_IncrementaContador()
    {
        // Arrange
        MetricasConexion metricas = new();

        // Act
        metricas.RegistrarError();

        // Assert
        metricas.Errores.Should().Be(1);
    }

    [Fact]
    public void ObtenerSnapshot_ConDatos_RetornaValoresCorrectos()
    {
        // Arrange
        MetricasConexion metricas = new();
        metricas.RegistrarServicioConectado();
        metricas.RegistrarServicioConectado();
        metricas.RegistrarBrowserConectado();
        metricas.RegistrarComandoEjecutado();
        metricas.RegistrarComandoEjecutado();
        metricas.RegistrarComandoEjecutado();
        metricas.RegistrarError();

        // Act
        object snapshot = metricas.ObtenerSnapshot();

        // Assert
        Type tipo = snapshot.GetType();
        long servicios = (long)tipo.GetProperty("serviciosConectados")!.GetValue(snapshot)!;
        long browsers = (long)tipo.GetProperty("browsersConectados")!.GetValue(snapshot)!;
        long comandos = (long)tipo.GetProperty("comandosEjecutados")!.GetValue(snapshot)!;
        long errores = (long)tipo.GetProperty("errores")!.GetValue(snapshot)!;
        servicios.Should().Be(2);
        browsers.Should().Be(1);
        comandos.Should().Be(3);
        errores.Should().Be(1);
    }

    [Fact]
    public void ContadoresIniciales_SinOperaciones_TodosEnCero()
    {
        // Arrange & Act
        MetricasConexion metricas = new();

        // Assert
        metricas.ServiciosConectados.Should().Be(0);
        metricas.BrowsersConectados.Should().Be(0);
        metricas.ComandosEjecutados.Should().Be(0);
        metricas.Errores.Should().Be(0);
    }
}
