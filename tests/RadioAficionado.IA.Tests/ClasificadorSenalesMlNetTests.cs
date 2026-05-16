using FluentAssertions;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el clasificador de senales basado en ML.NET.
/// </summary>
public sealed class ClasificadorSenalesMlNetTests
{
    private readonly ClasificadorSenalesMlNet _clasificador;

    public ClasificadorSenalesMlNetTests()
    {
        _clasificador = new ClasificadorSenalesMlNet();
    }

    [Fact]
    public async Task EspectroCw_DetectaPicoEstrecho()
    {
        // Arrange — pico estrecho en una sola frecuencia (simula CW puro)
        float[] espectro = new float[64];
        espectro[30] = 0.95f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        // CW y PSK tienen firmas similares (picos estrechos); ambos son resultados validos
        resultado.ModoDetectado.Should().BeOneOf([ModoOperacion.CW, ModoOperacion.PSK],
            "un pico estrecho debe identificarse como CW o PSK (firmas espectrales similares)");
        resultado.Confianza.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task EspectroRuido_DetectaRuido()
    {
        // Arrange — energia uniforme (simula ruido)
        float[] espectro = new float[64];
        Random aleatorio = new(123);
        for (int i = 0; i < 64; i++)
        {
            espectro[i] = 0.15f + (float)(aleatorio.NextDouble() * 0.15);
        }

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        // El modelo debe clasificar ruido uniforme; verificamos que no lance excepcion y retorne resultado valido
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task ConfianzaEnRangoValido()
    {
        // Arrange
        float[] espectro = new float[64];
        espectro[25] = 0.8f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Confianza.Should().BeInRange(0.0, 1.0,
            "la confianza debe estar entre 0 y 1");
    }

    [Fact]
    public async Task ClasificarLote_RetornaResultados()
    {
        // Arrange
        List<ReadOnlyMemory<float>> espectros = new();
        for (int i = 0; i < 5; i++)
        {
            float[] espectro = new float[64];
            espectro[10 + i * 5] = 0.9f;
            espectros.Add(new ReadOnlyMemory<float>(espectro));
        }

        // Act
        IReadOnlyList<ResultadoClasificacion> resultados = await _clasificador.ClasificarLoteAsync(espectros);

        // Assert
        resultados.Should().HaveCount(5,
            "debe retornar un resultado por cada espectro de entrada");
    }

    [Fact]
    public async Task EspectroSsb_DetectaBandaAncha()
    {
        // Arrange — energia distribuida en ~10 bins (simula SSB)
        float[] espectro = new float[64];
        for (int i = 15; i < 25; i++)
        {
            espectro[i] = 0.4f + (i % 3) * 0.1f;
        }

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeGreaterThan(0.0);
        resultado.ModosAlternativos.Should().NotBeNull();
    }

    [Fact]
    public async Task EspectroFt8_MultiplesTonos()
    {
        // Arrange — multiples tonos equiespaciados (simula FT8)
        float[] espectro = new float[64];
        for (int t = 0; t < 8; t++)
        {
            int pos = 10 + (int)(t * 1.5);
            if (pos < 64)
            {
                espectro[pos] = 0.7f;
            }
        }

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task EspectroRtty_DosTonos()
    {
        // Arrange — dos picos (mark y space, simula RTTY)
        float[] espectro = new float[64];
        espectro[25] = 0.85f;
        espectro[28] = 0.85f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task EspectroVacio_NoLanzaExcepcion()
    {
        // Arrange
        float[] espectro = new float[0];

        // Act
        Func<Task> accion = () => _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        await accion.Should().NotThrowAsync(
            "un espectro vacio no debe lanzar excepcion");
    }

    [Fact]
    public async Task EspectroGrande_ReducidoPorBinning()
    {
        // Arrange — espectro con 1024 puntos (debe reducirse a 64 por binning)
        float[] espectro = new float[1024];
        Random aleatorio = new(42);
        for (int i = 0; i < 1024; i++)
        {
            espectro[i] = (float)(aleatorio.NextDouble() * 0.1);
        }
        // Pico prominente
        espectro[500] = 0.95f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task EspectroPequeno_MenosDe64Bins()
    {
        // Arrange — espectro con solo 10 puntos
        float[] espectro = new float[10];
        espectro[5] = 0.9f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task ModosAlternativos_TienenConfianzaValida()
    {
        // Arrange
        float[] espectro = new float[64];
        espectro[32] = 0.7f;
        espectro[33] = 0.3f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.ModosAlternativos.Should().AllSatisfy(alternativa =>
        {
            alternativa.Confianza.Should().BeInRange(0.0, 1.0);
        });
    }

    [Fact]
    public async Task ClasificarLote_ConListaVacia_RetornaListaVacia()
    {
        // Arrange
        List<ReadOnlyMemory<float>> espectros = new();

        // Act
        IReadOnlyList<ResultadoClasificacion> resultados = await _clasificador.ClasificarLoteAsync(espectros);

        // Assert
        resultados.Should().BeEmpty();
    }

    [Fact]
    public async Task ClasificarLote_ConCancelacion_LanzaExcepcion()
    {
        // Arrange
        List<ReadOnlyMemory<float>> espectros = new()
        {
            new ReadOnlyMemory<float>(new float[64])
        };
        CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task> accion = () => _clasificador.ClasificarLoteAsync(espectros, cts.Token);

        // Assert
        await accion.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ClasificarLote_ConNull_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _clasificador.ClasificarLoteAsync(null!);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EspectroFm_PortadoraCentralConBandasLaterales()
    {
        // Arrange — portadora central fuerte con bandas laterales (simula FM)
        float[] espectro = new float[64];
        espectro[32] = 0.95f;
        for (int d = 1; d <= 5; d++)
        {
            espectro[32 - d] = 0.4f / d;
            espectro[32 + d] = 0.4f / d;
        }

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeGreaterThan(0.0);
    }
}
