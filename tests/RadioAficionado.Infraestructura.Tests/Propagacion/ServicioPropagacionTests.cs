using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;
using RadioAficionado.Infraestructura.Propagacion;
using Serilog;

namespace RadioAficionado.Infraestructura.Tests.Propagacion;

/// <summary>
/// Tests unitarios para <see cref="ServicioPropagacion"/>.
/// Valida el modelo de prediccion de propagacion HF basado en indices solares,
/// hora del dia, distancia y categoria de banda.
/// </summary>
public sealed class ServicioPropagacionTests : IDisposable
{
    private readonly ServicioPropagacion _servicio;
    private readonly HttpClient _clienteHttp;
    private readonly ConfiguracionPropagacion _configuracion;

    /// <summary>Coordenadas de Madrid, Espana.</summary>
    private readonly Coordenadas _madrid = new(40.4168, -3.7038);

    /// <summary>Coordenadas de Tokio, Japon.</summary>
    private readonly Coordenadas _tokio = new(35.6762, 139.6503);

    /// <summary>Coordenadas de Nueva York, Estados Unidos.</summary>
    private readonly Coordenadas _nuevaYork = new(40.7128, -74.0060);

    /// <summary>Coordenadas de Buenos Aires, Argentina.</summary>
    private readonly Coordenadas _buenosAires = new(-34.6037, -58.3816);

    public ServicioPropagacionTests()
    {
        ILogger logger = new LoggerConfiguration().CreateLogger();
        _clienteHttp = new HttpClient();
        _configuracion = new ConfiguracionPropagacion
        {
            IntervaloActualizacionMinutos = 30
        };
        _servicio = new ServicioPropagacion(logger, _clienteHttp, _configuracion);
    }

    public void Dispose()
    {
        _clienteHttp.Dispose();
    }

    // ─── SFI alto → bandas altas abiertas ───

    [Fact]
    public async Task PredecirPropagacionAsync_SfiAlto_BandasAltasAbiertas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 200, Kp: 1, Ap: 5, NumeroManchasSolares: 150.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, _tokio, medioDia);

        // Assert
        PrediccionBanda prediccion10m = predicciones.First(p => p.Banda == BandaRadio.Banda10m);
        ((int)prediccion10m.Nivel).Should().BeGreaterThanOrEqualTo((int)NivelPropagacion.Bueno,
            "con SFI=200 de dia, 10m deberia estar abierta");
    }

    [Fact]
    public async Task PredecirPropagacionAsync_SfiAlto_Banda15mAbierta()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 180, Kp: 1, Ap: 4, NumeroManchasSolares: 130.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 13, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, _nuevaYork, medioDia);

        // Assert
        PrediccionBanda prediccion15m = predicciones.First(p => p.Banda == BandaRadio.Banda15m);
        ((int)prediccion15m.Nivel).Should().BeGreaterThanOrEqualTo((int)NivelPropagacion.Bueno,
            "con SFI=180 de dia, 15m deberia tener buena propagacion");
    }

    // ─── SFI bajo → solo bandas bajas ───

    [Fact]
    public async Task PredecirPropagacionAsync_SfiBajo_BandasAltasCerradas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 70, Kp: 1, Ap: 3, NumeroManchasSolares: 10.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, _tokio, medioDia);

        // Assert
        PrediccionBanda prediccion10m = predicciones.First(p => p.Banda == BandaRadio.Banda10m);
        ((int)prediccion10m.Nivel).Should().BeLessThanOrEqualTo((int)NivelPropagacion.Pobre,
            "con SFI=70, 10m deberia estar cerrada o con propagacion pobre");
    }

    [Fact]
    public async Task PredecirPropagacionAsync_SfiBajo_Banda40mFunciona()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 75, Kp: 1, Ap: 3, NumeroManchasSolares: 15.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime noche = new(2026, 3, 23, 2, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, noche);

        // Assert
        PrediccionBanda prediccion40m = predicciones.First(p => p.Banda == BandaRadio.Banda40m);
        ((int)prediccion40m.Nivel).Should().BeGreaterThanOrEqualTo((int)NivelPropagacion.Regular,
            "40m de noche con SFI bajo deberia funcionar bien");
    }

    // ─── Prediccion nocturna → bandas bajas favorecidas ───

    [Fact]
    public async Task PredecirPropagacionAsync_Nocturno_BandasBajasFavorecidas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 100, Kp: 1, Ap: 5, NumeroManchasSolares: 50.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime noche = new(2026, 3, 23, 3, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, noche);

        // Assert
        PrediccionBanda prediccion80m = predicciones.First(p => p.Banda == BandaRadio.Banda80m);
        PrediccionBanda prediccion10m = predicciones.First(p => p.Banda == BandaRadio.Banda10m);
        ((int)prediccion80m.Nivel).Should().BeGreaterThan((int)prediccion10m.Nivel,
            "de noche, 80m deberia tener mejor propagacion que 10m");
    }

    [Fact]
    public async Task PredecirPropagacionAsync_Nocturno_Banda160mActiva()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 90, Kp: 0, Ap: 3, NumeroManchasSolares: 40.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medianoche = new(2026, 3, 23, 1, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, medianoche);

        // Assert
        PrediccionBanda prediccion160m = predicciones.First(p => p.Banda == BandaRadio.Banda160m);
        ((int)prediccion160m.Nivel).Should().BeGreaterThanOrEqualTo((int)NivelPropagacion.Regular,
            "160m de noche con condiciones tranquilas deberia funcionar");
    }

    // ─── Prediccion diurna → bandas altas favorecidas ───

    [Fact]
    public async Task PredecirPropagacionAsync_Diurno_BandasMediasFavorecidas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 130, Kp: 1, Ap: 5, NumeroManchasSolares: 80.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, medioDia);

        // Assert
        PrediccionBanda prediccion20m = predicciones.First(p => p.Banda == BandaRadio.Banda20m);
        ((int)prediccion20m.Nivel).Should().BeGreaterThanOrEqualTo((int)NivelPropagacion.Bueno,
            "20m de dia con SFI=130 deberia tener buena propagacion");
    }

    [Fact]
    public async Task PredecirPropagacionAsync_Diurno_BandasBajasDegradadas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 160, Kp: 1, Ap: 5, NumeroManchasSolares: 100.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, medioDia);

        // Assert
        PrediccionBanda prediccion80m = predicciones.First(p => p.Banda == BandaRadio.Banda80m);
        PrediccionBanda prediccion20m = predicciones.First(p => p.Banda == BandaRadio.Banda20m);
        ((int)prediccion20m.Nivel).Should().BeGreaterThanOrEqualTo((int)prediccion80m.Nivel,
            "de dia con SFI alto, 20m deberia ser igual o mejor que 80m");
    }

    // ─── Mejor banda para distancia corta vs larga ───

    [Fact]
    public async Task ObtenerMejorBandaAsync_DistanciaCorta_PrefiereMediasOBajas()
    {
        // Arrange — Madrid a Nueva York: ~5700 km
        IndicesSolares indices = new(Sfi: 120, Kp: 1, Ap: 5, NumeroManchasSolares: 70.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 14, 0, 0, DateTimeKind.Utc);

        // Act
        BandaRadio? mejorBanda = await _servicio.ObtenerMejorBandaAsync(_madrid, _nuevaYork, medioDia);

        // Assert
        mejorBanda.Should().NotBeNull("deberia haber al menos una banda abierta");
        BandaRadio[] bandasEsperadas = [BandaRadio.Banda20m, BandaRadio.Banda17m, BandaRadio.Banda30m, BandaRadio.Banda15m];
        bandasEsperadas.Should().Contain(mejorBanda!.Value,
            "para distancia media-larga de dia, deberian funcionar las bandas medias/altas");
    }

    [Fact]
    public async Task ObtenerMejorBandaAsync_DistanciaLarga_PrefiereMedias()
    {
        // Arrange — Madrid a Tokio: ~10,500 km
        IndicesSolares indices = new(Sfi: 140, Kp: 1, Ap: 5, NumeroManchasSolares: 90.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 10, 0, 0, DateTimeKind.Utc);

        // Act
        BandaRadio? mejorBanda = await _servicio.ObtenerMejorBandaAsync(_madrid, _tokio, medioDia);

        // Assert
        mejorBanda.Should().NotBeNull("deberia haber al menos una banda abierta para DX");
    }

    // ─── Cache de indices funciona ───

    [Fact]
    public async Task ObtenerIndicesSolaresAsync_ConsultaRepetida_UsaCache()
    {
        // Arrange
        IndicesSolares indicesInyectados = new(Sfi: 175, Kp: 3, Ap: 12, NumeroManchasSolares: 110.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indicesInyectados);

        // Act
        IndicesSolares primera = await _servicio.ObtenerIndicesSolaresAsync();
        IndicesSolares segunda = await _servicio.ObtenerIndicesSolaresAsync();

        // Assert
        primera.Should().Be(segunda, "la segunda consulta deberia devolver el mismo objeto cacheado");
        primera.Sfi.Should().Be(175);
    }

    [Fact]
    public async Task ObtenerIndicesSolaresAsync_DespuesDeInvalidar_NoUsaCache()
    {
        // Arrange
        IndicesSolares indicesOriginales = new(Sfi: 175, Kp: 3, Ap: 12, NumeroManchasSolares: 110.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indicesOriginales);

        IndicesSolares primeraConsulta = await _servicio.ObtenerIndicesSolaresAsync();

        // Act
        _servicio.InvalidarCache();
        // Tras invalidar, al consultar sin conexion real devolvera el fallback
        IndicesSolares segundaConsulta = await _servicio.ObtenerIndicesSolaresAsync();

        // Assert
        primeraConsulta.Sfi.Should().Be(175);
        // El fallback tiene SFI=100 porque no hay conexion HTTP real
        segundaConsulta.Sfi.Should().NotBe(175,
            "tras invalidar cache, no deberia devolver los indices anteriores");
    }

    // ─── Predicciones para diferentes horas del dia ───

    [Fact]
    public async Task PredecirPropagacionAsync_Amanecer_TransicionBandas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 120, Kp: 1, Ap: 5, NumeroManchasSolares: 70.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime amanecer = new(2026, 3, 23, 6, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, amanecer);

        // Assert
        predicciones.Should().HaveCount(10, "deberia evaluar las 10 bandas HF");
        predicciones.Should().OnlyContain(p => p.Descripcion.Length > 0, "todas las predicciones deben tener descripcion");
    }

    [Fact]
    public async Task PredecirPropagacionAsync_Atardecer_BandasMediasFuncionan()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 110, Kp: 2, Ap: 7, NumeroManchasSolares: 60.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime atardecer = new(2026, 3, 23, 18, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, _buenosAires, atardecer);

        // Assert
        PrediccionBanda prediccion20m = predicciones.First(p => p.Banda == BandaRadio.Banda20m);
        ((int)prediccion20m.Nivel).Should().BeGreaterThanOrEqualTo((int)NivelPropagacion.Pobre,
            "20m al atardecer todavia deberia tener algo de propagacion");
    }

    // ─── Perturbacion geomagnetica ───

    [Fact]
    public async Task PredecirPropagacionAsync_KpAlto_DegradaPropagacion()
    {
        // Arrange — Mismas condiciones pero con tormenta geomagnetica
        IndicesSolares indicesTranquilos = new(Sfi: 150, Kp: 1, Ap: 5, NumeroManchasSolares: 100.0, FechaActualizacion: DateTime.UtcNow);
        IndicesSolares indicesPerturbados = new(Sfi: 150, Kp: 7, Ap: 50, NumeroManchasSolares: 100.0, FechaActualizacion: DateTime.UtcNow);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        _servicio.EstablecerIndicesSolares(indicesTranquilos);
        IReadOnlyList<PrediccionBanda> prediccionesTranquilas = await _servicio.PredecirPropagacionAsync(_madrid, _tokio, medioDia);

        _servicio.EstablecerIndicesSolares(indicesPerturbados);
        IReadOnlyList<PrediccionBanda> prediccionesPerturbadas = await _servicio.PredecirPropagacionAsync(_madrid, _tokio, medioDia);

        // Assert
        int sumaTranquila = prediccionesTranquilas.Sum(p => (int)p.Nivel);
        int sumaPerturbada = prediccionesPerturbadas.Sum(p => (int)p.Nivel);
        sumaPerturbada.Should().BeLessThan(sumaTranquila,
            "con Kp=7 la propagacion total deberia ser peor que con Kp=1");
    }

    // ─── Regiones alcanzables ───

    [Fact]
    public async Task PredecirPropagacionAsync_NivelExcelente_IncluyeRegionesGlobales()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 200, Kp: 0, Ap: 2, NumeroManchasSolares: 160.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, _tokio, medioDia);

        // Assert
        PrediccionBanda mejorPrediccion = predicciones.OrderByDescending(p => p.Nivel).First();
        mejorPrediccion.RegionesAlcanzables.Should().Contain("Continental",
            "con excelentes condiciones, al menos deberia alcanzar nivel continental");
    }

    [Fact]
    public async Task PredecirPropagacionAsync_NivelNulo_SinRegiones()
    {
        // Arrange — SFI muy bajo + noche = 10m cerrada
        IndicesSolares indices = new(Sfi: 65, Kp: 5, Ap: 30, NumeroManchasSolares: 5.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime noche = new(2026, 3, 23, 2, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, noche);

        // Assert
        PrediccionBanda prediccion10m = predicciones.First(p => p.Banda == BandaRadio.Banda10m);
        prediccion10m.Nivel.Should().Be(NivelPropagacion.Nulo);
        prediccion10m.RegionesAlcanzables.Should().BeEmpty(
            "sin propagacion no deberia haber regiones alcanzables");
    }

    // ─── Validacion de IndicesSolares ───

    [Fact]
    public void IndicesSolares_ValoresValidos_EsValidoTrue()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 120, Kp: 3, Ap: 10, NumeroManchasSolares: 80.0, FechaActualizacion: DateTime.UtcNow);

        // Act & Assert
        indices.EsValido().Should().BeTrue();
    }

    [Fact]
    public void IndicesSolares_SfiFueraDeRango_EsValidoFalse()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 50, Kp: 3, Ap: 10, NumeroManchasSolares: 80.0, FechaActualizacion: DateTime.UtcNow);

        // Act & Assert
        indices.EsValido().Should().BeFalse("SFI=50 esta por debajo del minimo de 60");
    }

    // ─── ObtenerMejorBanda con condiciones extremas ───

    [Fact]
    public async Task ObtenerMejorBandaAsync_CondicionesExtremas_SinPropagacionExcelente()
    {
        // Arrange — condiciones extremadamente malas
        IndicesSolares indices = new(Sfi: 60, Kp: 9, Ap: 100, NumeroManchasSolares: 0.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime medioDia = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, _tokio, medioDia);

        // Assert
        predicciones.Should().NotContain(p => p.Nivel == NivelPropagacion.Excelente,
            "con Kp=9, ninguna banda deberia tener propagacion excelente");
    }

    // ─── Ventana horaria optima ───

    [Fact]
    public async Task PredecirPropagacionAsync_VentanaHoraria_BandasBajasNocturnas()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 100, Kp: 1, Ap: 5, NumeroManchasSolares: 50.0, FechaActualizacion: DateTime.UtcNow);
        _servicio.EstablecerIndicesSolares(indices);
        DateTime hora = new(2026, 3, 23, 12, 0, 0, DateTimeKind.Utc);

        // Act
        IReadOnlyList<PrediccionBanda> predicciones = await _servicio.PredecirPropagacionAsync(_madrid, null, hora);

        // Assert
        PrediccionBanda prediccion80m = predicciones.First(p => p.Banda == BandaRadio.Banda80m);
        prediccion80m.MejorHoraInicio.Should().Be(new TimeSpan(22, 0, 0),
            "la mejor hora de 80m deberia empezar a las 22 UTC");
        prediccion80m.MejorHoraFin.Should().Be(new TimeSpan(6, 0, 0),
            "la mejor hora de 80m deberia terminar a las 06 UTC");
    }

    // ─── Propiedades de IndicesSolares ───

    [Fact]
    public void IndicesSolares_FlujoAlto_IndicaCorrectamente()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 160, Kp: 1, Ap: 5, NumeroManchasSolares: 100.0, FechaActualizacion: DateTime.UtcNow);

        // Act & Assert
        indices.FlujoSolarAlto.Should().BeTrue("SFI=160 es mayor a 150");
        indices.FlujoSolarBajo.Should().BeFalse("SFI=160 no es menor a 90");
        indices.CondicionesPerturbadas.Should().BeFalse("Kp=1 no indica perturbacion");
    }
}
