using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.ObjetosDeValor;

/// <summary>
/// Tests unitarios para el objeto de valor <see cref="Coordenadas"/>.
/// Valida la construcción con rangos válidos e inválidos, cálculo de distancia
/// mediante Haversine y conversión a localizador Maidenhead.
/// </summary>
public class CoordenadasTests
{
    [Fact]
    public void Constructor_ValoresValidos_CreaCoordenadas()
    {
        // Arrange
        double latitud = 40.4168;
        double longitud = -3.7038;

        // Act
        Coordenadas coordenadas = new Coordenadas(latitud, longitud);

        // Assert
        coordenadas.Latitud.Should().Be(40.4168);
        coordenadas.Longitud.Should().Be(-3.7038);
    }

    [Theory]
    [InlineData(91.0)]
    [InlineData(-91.0)]
    public void Constructor_LatitudFueraDeRango_LanzaArgumentOutOfRangeException(double latitud)
    {
        // Arrange & Act
        Action accion = () => new Coordenadas(latitud, 0.0);

        // Assert
        accion.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(181.0)]
    [InlineData(-181.0)]
    public void Constructor_LongitudFueraDeRango_LanzaArgumentOutOfRangeException(double longitud)
    {
        // Arrange & Act
        Action accion = () => new Coordenadas(0.0, longitud);

        // Assert
        accion.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalcularDistancia_MadridNuevaYork_DistanciaAproximada()
    {
        // Arrange
        Coordenadas madrid = new Coordenadas(40.4168, -3.7038);
        Coordenadas nuevaYork = new Coordenadas(40.7128, -74.0060);

        // Act
        double distancia = madrid.CalcularDistancia(nuevaYork);

        // Assert
        distancia.Should().BeApproximately(5770.0, 100.0);
    }

    [Fact]
    public void CalcularDistancia_MismoPunto_DevuelveCero()
    {
        // Arrange
        Coordenadas punto = new Coordenadas(40.4168, -3.7038);

        // Act
        double distancia = punto.CalcularDistancia(punto);

        // Assert
        distancia.Should().Be(0.0);
    }

    [Fact]
    public void ObtenerLocalizador_CoordenadasConocidas_DevuelveLocalizadorCorrecto()
    {
        // Arrange
        Coordenadas madrid = new Coordenadas(40.4168, -3.7038);

        // Act
        Localizador localizador = madrid.ObtenerLocalizador();

        // Assert
        localizador.Valor.Should().StartWith("IN80");
    }
}
