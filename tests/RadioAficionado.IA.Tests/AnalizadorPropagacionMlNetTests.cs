using FluentAssertions;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el analizador de propagacion basado en ML.NET.
/// </summary>
public sealed class AnalizadorPropagacionMlNetTests
{
    private readonly AnalizadorPropagacionMlNet _analizador;

    public AnalizadorPropagacionMlNetTests()
    {
        _analizador = new AnalizadorPropagacionMlNet();
    }

    [Fact]
    public async Task SfiAlto_BandaAlta_ProbabilidadAlta()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 200, Kp: 1, Ap: 5, NumeroManchasSolares: 150, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultado = await _analizador.PredecirAsync(indices, BandaRadio.Banda10m);

        // Assert
        resultado.Banda.Should().Be(BandaRadio.Banda10m);
        resultado.ProbabilidadApertura.Should().BeGreaterThan(0.3,
            "un SFI de 200 deberia favorecer la apertura de bandas altas como 10m");
    }

    [Fact]
    public async Task SfiBajo_BandaBaja_ProbabilidadAlta()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 70, Kp: 1, Ap: 3, NumeroManchasSolares: 20, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultado = await _analizador.PredecirAsync(indices, BandaRadio.Banda40m);

        // Assert
        resultado.Banda.Should().Be(BandaRadio.Banda40m);
        resultado.ProbabilidadApertura.Should().BeGreaterThan(0.2,
            "las bandas bajas como 40m deben permanecer abiertas incluso con SFI bajo");
    }

    [Fact]
    public async Task PredecirTodasLasBandas_RetornaListaCompleta()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 120, Kp: 2, Ap: 8, NumeroManchasSolares: 80, FechaActualizacion: DateTime.UtcNow);

        // Act
        IReadOnlyList<PrediccionPropagacionIa> resultados = await _analizador.PredecirTodasLasBandasAsync(indices);

        // Assert
        resultados.Should().HaveCount(10, "debe haber predicciones para las 10 bandas HF principales");
    }

    [Theory]
    [InlineData(BandaRadio.Banda160m)]
    [InlineData(BandaRadio.Banda80m)]
    [InlineData(BandaRadio.Banda40m)]
    [InlineData(BandaRadio.Banda20m)]
    [InlineData(BandaRadio.Banda15m)]
    [InlineData(BandaRadio.Banda10m)]
    public async Task ProbabilidadEnRangoValido_ParaCualquierBanda(BandaRadio banda)
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 130, Kp: 2, Ap: 10, NumeroManchasSolares: 90, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultado = await _analizador.PredecirAsync(indices, banda);

        // Assert
        resultado.ProbabilidadApertura.Should().BeInRange(0.0, 1.0,
            "la probabilidad debe estar siempre entre 0 y 1");
    }

    [Theory]
    [InlineData(BandaRadio.Banda160m)]
    [InlineData(BandaRadio.Banda80m)]
    [InlineData(BandaRadio.Banda40m)]
    [InlineData(BandaRadio.Banda20m)]
    [InlineData(BandaRadio.Banda15m)]
    [InlineData(BandaRadio.Banda10m)]
    public async Task NivelConfianzaEnRangoValido_ParaCualquierBanda(BandaRadio banda)
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 100, Kp: 1, Ap: 5, NumeroManchasSolares: 60, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultado = await _analizador.PredecirAsync(indices, banda);

        // Assert
        resultado.NivelConfianza.Should().BeInRange(0.0, 1.0,
            "el nivel de confianza debe estar siempre entre 0 y 1");
    }

    [Theory]
    [InlineData(60, 0, 0, 0)]
    [InlineData(300, 9, 100, 500)]
    [InlineData(65, 9, 400, 0)]
    [InlineData(250, 0, 0, 300)]
    public async Task ConIndicesExtremos_NoLanzaExcepcion(int sfi, int kp, int ap, double manchas)
    {
        // Arrange
        IndicesSolares indices = new(Sfi: sfi, Kp: kp, Ap: ap, NumeroManchasSolares: manchas, FechaActualizacion: DateTime.UtcNow);

        // Act
        Func<Task> accion = () => _analizador.PredecirAsync(indices, BandaRadio.Banda20m);

        // Assert
        await accion.Should().NotThrowAsync(
            "el modelo debe manejar indices extremos sin lanzar excepciones");
    }

    [Fact]
    public async Task PredecirTodasLasBandas_ContieneTodasLasBandasHf()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 150, Kp: 1, Ap: 5, NumeroManchasSolares: 100, FechaActualizacion: DateTime.UtcNow);
        BandaRadio[] bandasEsperadas =
        [
            BandaRadio.Banda160m, BandaRadio.Banda80m, BandaRadio.Banda60m,
            BandaRadio.Banda40m, BandaRadio.Banda30m, BandaRadio.Banda20m,
            BandaRadio.Banda17m, BandaRadio.Banda15m, BandaRadio.Banda12m,
            BandaRadio.Banda10m
        ];

        // Act
        IReadOnlyList<PrediccionPropagacionIa> resultados = await _analizador.PredecirTodasLasBandasAsync(indices);

        // Assert
        IEnumerable<BandaRadio> bandasEnResultado = resultados.Select(r => r.Banda);
        bandasEnResultado.Should().BeEquivalentTo(bandasEsperadas,
            "debe incluir todas las bandas HF principales");
    }

    [Fact]
    public async Task ConPerturbacionGeomagnetica_ConfianzaReducida()
    {
        // Arrange
        IndicesSolares indicesEstables = new(Sfi: 130, Kp: 1, Ap: 5, NumeroManchasSolares: 80, FechaActualizacion: DateTime.UtcNow);
        IndicesSolares indicesPerturbados = new(Sfi: 130, Kp: 7, Ap: 50, NumeroManchasSolares: 80, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultadoEstable = await _analizador.PredecirAsync(indicesEstables, BandaRadio.Banda20m);
        PrediccionPropagacionIa resultadoPerturbado = await _analizador.PredecirAsync(indicesPerturbados, BandaRadio.Banda20m);

        // Assert
        resultadoPerturbado.NivelConfianza.Should().BeLessThan(resultadoEstable.NivelConfianza,
            "las perturbaciones geomagneticas deben reducir la confianza del modelo");
    }

    [Fact]
    public async Task HoraOptima_BandaBaja_EsNocturna()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 100, Kp: 2, Ap: 8, NumeroManchasSolares: 60, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultado = await _analizador.PredecirAsync(indices, BandaRadio.Banda160m);

        // Assert
        resultado.HoraOptima.Should().NotBeNull();
        resultado.HoraOptima!.Value.Hour.Should().BeOneOf([0, 1, 2, 3, 4, 5, 21, 22, 23],
            "la hora optima para 160m debe ser nocturna");
    }

    [Fact]
    public async Task HoraOptima_BandaAlta_EsDiurna()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 180, Kp: 1, Ap: 3, NumeroManchasSolares: 130, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultado = await _analizador.PredecirAsync(indices, BandaRadio.Banda10m);

        // Assert
        resultado.HoraOptima.Should().NotBeNull();
        resultado.HoraOptima!.Value.Hour.Should().BeInRange(8, 16,
            "la hora optima para 10m debe ser diurna");
    }

    [Fact]
    public async Task MufEstimado_SfiAlto_MayorQueSfiBajo()
    {
        // Arrange
        IndicesSolares indicesAltos = new(Sfi: 200, Kp: 1, Ap: 5, NumeroManchasSolares: 150, FechaActualizacion: DateTime.UtcNow);
        IndicesSolares indicesBajos = new(Sfi: 70, Kp: 1, Ap: 5, NumeroManchasSolares: 20, FechaActualizacion: DateTime.UtcNow);

        // Act
        PrediccionPropagacionIa resultadoAlto = await _analizador.PredecirAsync(indicesAltos, BandaRadio.Banda20m);
        PrediccionPropagacionIa resultadoBajo = await _analizador.PredecirAsync(indicesBajos, BandaRadio.Banda20m);

        // Assert
        resultadoAlto.MufEstimado.Should().NotBeNull();
        resultadoBajo.MufEstimado.Should().NotBeNull();
        resultadoAlto.MufEstimado!.Value.Should().BeGreaterThan(resultadoBajo.MufEstimado!.Value,
            "un SFI mas alto deberia producir una MUF estimada mayor");
    }

    [Fact]
    public async Task PredecirAsync_ConBandaNoHf_NoLanzaExcepcion()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 130, Kp: 2, Ap: 8, NumeroManchasSolares: 90, FechaActualizacion: DateTime.UtcNow);

        // Act
        Func<Task> accion = () => _analizador.PredecirAsync(indices, BandaRadio.Banda2m);

        // Assert
        await accion.Should().NotThrowAsync(
            "el modelo debe manejar bandas no-HF sin lanzar excepciones");
    }

    [Fact]
    public async Task PredecirAsync_ConIndicesNull_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _analizador.PredecirAsync(null!, BandaRadio.Banda20m);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PredecirTodasLasBandas_ConCancelacion_LanzaExcepcion()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 130, Kp: 2, Ap: 8, NumeroManchasSolares: 90, FechaActualizacion: DateTime.UtcNow);
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task> accion = () => _analizador.PredecirTodasLasBandasAsync(indices, cts.Token);

        // Assert
        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PredecirTodasLasBandas_TodasLasProbabilidadesEnRango()
    {
        // Arrange
        IndicesSolares indices = new(Sfi: 100, Kp: 3, Ap: 12, NumeroManchasSolares: 60, FechaActualizacion: DateTime.UtcNow);

        // Act
        IReadOnlyList<PrediccionPropagacionIa> resultados = await _analizador.PredecirTodasLasBandasAsync(indices);

        // Assert
        resultados.Should().AllSatisfy(r =>
        {
            r.ProbabilidadApertura.Should().BeInRange(0.0, 1.0);
            r.NivelConfianza.Should().BeInRange(0.0, 1.0);
        });
    }
}
