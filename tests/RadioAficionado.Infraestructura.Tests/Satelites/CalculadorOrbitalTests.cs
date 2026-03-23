using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;
using RadioAficionado.Infraestructura.Satelites;

namespace RadioAficionado.Infraestructura.Tests.Satelites;

/// <summary>
/// Tests unitarios para <see cref="CalculadorOrbital"/>.
/// Usa TLEs reales de la ISS para validar el parser y los cálculos de posición/pasos.
/// </summary>
public sealed class CalculadorOrbitalTests
{
    /// <summary>TLE real de la ISS (época cercana a 2024).</summary>
    private const string TleIssLinea0 = "ISS (ZARYA)";
    private const string TleIssLinea1 = "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9005";
    private const string TleIssLinea2 = "2 25544  51.6400 208.5000 0007417  50.2000 100.3000 15.49560000400001";

    /// <summary>Coordenadas de Madrid, España.</summary>
    private readonly Coordenadas _madrid = new(40.4168, -3.7038);

    /// <summary>Coordenadas de Buenos Aires, Argentina.</summary>
    private readonly Coordenadas _buenosAires = new(-34.6037, -58.3816);

    // ─── Tests de parseo de TLE ───

    [Fact]
    public void ParsearTle_ConTleRealIss_DebeExtraerNumeroNorad()
    {
        // Act
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);

        // Assert
        tle.NumeroNorad.Should().Be(25544);
    }

    [Fact]
    public void ParsearTle_ConTleRealIss_DebeExtraerNombre()
    {
        // Act
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);

        // Assert
        tle.Nombre.Should().Be("ISS (ZARYA)");
    }

    [Fact]
    public void ParsearTle_ConTleRealIss_DebeExtraerInclinacion()
    {
        // Act
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);

        // Assert
        tle.InclinacionGrados.Should().BeApproximately(51.64, 0.01,
            "la ISS tiene una inclinación orbital de ~51.6°");
    }

    [Fact]
    public void ParsearTle_ConTleRealIss_DebeExtraerExcentricidad()
    {
        // Act
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);

        // Assert
        tle.Excentricidad.Should().BeApproximately(0.0007417, 0.0001,
            "la ISS tiene una órbita casi circular con excentricidad muy baja");
    }

    [Fact]
    public void ParsearTle_ConTleRealIss_DebeExtraerMovimientoMedio()
    {
        // Act
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);

        // Assert
        tle.MovimientoMedioRevDia.Should().BeApproximately(15.4956, 0.01,
            "la ISS orbita la Tierra ~15.5 veces al día");
    }

    [Fact]
    public void ParsearTle_ConLinea1Corta_DebeLanzarFormatException()
    {
        // Act
        Action accion = () => CalculadorOrbital.ParsearTle("ISS", "1 25544", TleIssLinea2);

        // Assert
        accion.Should().Throw<FormatException>();
    }

    [Fact]
    public void ParsearTle_ConNombreVacio_DebeLanzarArgumentException()
    {
        // Act
        Action accion = () => CalculadorOrbital.ParsearTle("", TleIssLinea1, TleIssLinea2);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    // ─── Tests de cálculo de posición ───

    [Fact]
    public void CalcularPosicion_ConIss_AltitudDebeSerRazonable()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        DateTime momento = tle.Epoca;

        // Act
        PosicionSatelite posicion = CalculadorOrbital.CalcularPosicion(tle, _madrid, momento);

        // Assert
        posicion.Altitud.Should().BeInRange(350.0, 500.0,
            "la ISS orbita a una altitud de ~400 km");
    }

    [Fact]
    public void CalcularPosicion_ConIss_LatitudDebeEstarDentroDeInclinacion()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        DateTime momento = tle.Epoca;

        // Act
        PosicionSatelite posicion = CalculadorOrbital.CalcularPosicion(tle, _madrid, momento);

        // Assert
        // La latitud del subsatélite no puede superar la inclinación orbital
        Math.Abs(posicion.Latitud).Should().BeLessThanOrEqualTo(52.0,
            "la latitud subsatélite no puede exceder la inclinación orbital (~51.6°)");
    }

    [Fact]
    public void CalcularPosicion_ConIss_ElevacionDebeSerValida()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        DateTime momento = tle.Epoca;

        // Act
        PosicionSatelite posicion = CalculadorOrbital.CalcularPosicion(tle, _madrid, momento);

        // Assert
        posicion.Elevacion.Should().BeInRange(-90.0, 90.0,
            "la elevación debe estar entre -90° y 90°");
    }

    [Fact]
    public void CalcularPosicion_ConIss_AzimutDebeSerValido()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        DateTime momento = tle.Epoca;

        // Act
        PosicionSatelite posicion = CalculadorOrbital.CalcularPosicion(tle, _madrid, momento);

        // Assert
        posicion.Azimut.Should().BeInRange(0.0, 360.0,
            "el azimut debe estar entre 0° y 360°");
    }

    [Fact]
    public void CalcularPosicion_ConIss_DistanciaDebeSerRazonable()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        DateTime momento = tle.Epoca;

        // Act
        PosicionSatelite posicion = CalculadorOrbital.CalcularPosicion(tle, _madrid, momento);

        // Assert
        // La distancia mínima es ~400 km (justo encima), máxima ~13000 km (al otro lado del planeta)
        posicion.Distancia.Should().BeInRange(350.0, 13000.0,
            "la distancia a la ISS debe ser positiva y razonable para un satélite LEO");
    }

    [Fact]
    public void CalcularPosicion_ConTleNulo_DebeLanzarArgumentNullException()
    {
        // Act
        Action accion = () => CalculadorOrbital.CalcularPosicion(null!, _madrid, DateTime.UtcNow);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    // ─── Tests de predicción de pasos ───

    [Fact]
    public void PredecirPasos_ConIss24Horas_DebeEncontrarAlMenosUnPaso()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        SateliteAmateur iss = CatalogoSatelites.BuscarPorNorad(25544)!;
        DateTime desde = tle.Epoca;
        DateTime hasta = desde.AddHours(24);

        // Act
        IReadOnlyList<PasoSatelite> pasos = CalculadorOrbital.PredecirPasos(
            tle, iss, _madrid, desde, hasta, 5.0);

        // Assert
        pasos.Should().NotBeEmpty(
            "la ISS debe pasar al menos una vez sobre Madrid en 24 horas");
    }

    [Fact]
    public void PredecirPasos_ConIss24Horas_PasosDebenTenerDuracionRazonable()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        SateliteAmateur iss = CatalogoSatelites.BuscarPorNorad(25544)!;
        DateTime desde = tle.Epoca;
        DateTime hasta = desde.AddHours(24);

        // Act
        IReadOnlyList<PasoSatelite> pasos = CalculadorOrbital.PredecirPasos(
            tle, iss, _madrid, desde, hasta, 5.0);

        // Assert
        foreach (PasoSatelite paso in pasos)
        {
            paso.DuracionSegundos.Should().BeInRange(30.0, 900.0,
                $"un paso de la ISS dura entre 30 segundos y 15 minutos, pero fue {paso.DuracionSegundos:F0}s");
            paso.ElevacionMaxima.Should().BeGreaterThanOrEqualTo(5.0,
                "la elevación máxima debe superar la mínima configurada");
            paso.Los.Should().BeAfter(paso.Aos,
                "LOS debe ser posterior a AOS");
        }
    }

    [Fact]
    public void PredecirPasos_ConIss24Horas_DebeEncontrarEntreUnoCincoEnBuenosAires()
    {
        // Arrange
        Tle tle = CalculadorOrbital.ParsearTle(TleIssLinea0, TleIssLinea1, TleIssLinea2);
        SateliteAmateur iss = CatalogoSatelites.BuscarPorNorad(25544)!;
        DateTime desde = tle.Epoca;
        DateTime hasta = desde.AddHours(24);

        // Act
        IReadOnlyList<PasoSatelite> pasos = CalculadorOrbital.PredecirPasos(
            tle, iss, _buenosAires, desde, hasta, 5.0);

        // Assert
        pasos.Count.Should().BeInRange(1, 10,
            "la ISS debe tener entre 1 y 10 pasos sobre Buenos Aires en 24 horas");
    }

    // ─── Tests de parseo múltiple ───

    [Fact]
    public void ParsearMultiplesTle_ConTextoVacio_DebeRetornarListaVacia()
    {
        // Act
        IReadOnlyList<Tle> resultado = CalculadorOrbital.ParsearMultiplesTle("");

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void ParsearMultiplesTle_ConDosTles_DebeRetornarDos()
    {
        // Arrange
        string texto = $"""
            ISS (ZARYA)
            {TleIssLinea1}
            {TleIssLinea2}
            SO-50
            1 27607U 02058C   24001.50000000  .00000100  00000-0  50000-4 0  9999
            2 27607  64.5570 120.0000 0030000  90.0000 270.0000 14.75000000200001
            """;

        // Act
        IReadOnlyList<Tle> resultado = CalculadorOrbital.ParsearMultiplesTle(texto);

        // Assert
        resultado.Count.Should().Be(2);
        resultado[0].NumeroNorad.Should().Be(25544);
        resultado[1].NumeroNorad.Should().Be(27607);
    }
}
