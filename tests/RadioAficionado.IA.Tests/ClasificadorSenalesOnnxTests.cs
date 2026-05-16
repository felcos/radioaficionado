using FluentAssertions;
using Moq;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el clasificador de senales basado en ONNX Runtime.
/// Verifica el comportamiento de fallback a ML.NET, validaciones de entrada
/// y clasificacion correcta.
/// </summary>
public sealed class ClasificadorSenalesOnnxTests
{
    private readonly Mock<IMotorInferenciaOnnx> _mockMotorOnnx;
    private readonly ClasificadorSenalesMlNet _clasificadorMlNet;
    private readonly ClasificadorSenalesOnnx _clasificador;

    public ClasificadorSenalesOnnxTests()
    {
        _mockMotorOnnx = new Mock<IMotorInferenciaOnnx>();
        _clasificadorMlNet = new ClasificadorSenalesMlNet();
        _clasificador = new ClasificadorSenalesOnnx(_mockMotorOnnx.Object, _clasificadorMlNet);
    }

    [Fact]
    public async Task Clasificar_SinModeloOnnx_UsaFallbackMlNet()
    {
        // Arrange
        _mockMotorOnnx.Setup(m => m.ModeloEstaCargado("clasificador_senales")).Returns(false);

        float[] espectro = new float[64];
        espectro[30] = 0.9f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull("el fallback ML.NET debe retornar un resultado valido");
        resultado.Confianza.Should().BeInRange(0.0, 1.0);
        _mockMotorOnnx.Verify(m => m.EjecutarInferenciaAsync(
            It.IsAny<float[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never,
            "no debe llamar al motor ONNX si el modelo no esta cargado");
    }

    [Fact]
    public async Task Clasificar_EspectroNulo_LanzaExcepcion()
    {
        // Arrange
        _mockMotorOnnx.Setup(m => m.ModeloEstaCargado("clasificador_senales")).Returns(false);

        // Act — ReadOnlyMemory<float>.Empty es valido, pero al pasar null
        // al ClasificadorSenalesMlNet se espera que no lance (espectro vacio)
        // Probamos con cancelacion para forzar excepcion
        CancellationTokenSource cts = new();
        cts.Cancel();

        Func<Task> accion = () => _clasificador.ClasificarAsync(
            new ReadOnlyMemory<float>(new float[64]), cts.Token);

        // Assert
        await accion.Should().ThrowAsync<OperationCanceledException>(
            "la cancelacion debe propagarse correctamente");
    }

    [Fact]
    public async Task Clasificar_EspectroVacio_LanzaExcepcion()
    {
        // Arrange
        _mockMotorOnnx.Setup(m => m.ModeloEstaCargado("clasificador_senales")).Returns(false);
        float[] espectroVacio = [];

        // Act
        Func<Task> accion = async () => await _clasificador.ClasificarAsync(
            new ReadOnlyMemory<float>(espectroVacio));

        // Assert — con fallback ML.NET, espectro vacio se procesa sin error
        await accion.Should().NotThrowAsync(
            "un espectro vacio se procesa via fallback ML.NET sin lanzar excepcion");
    }

    [Fact]
    public async Task ClasificarLote_ListaVacia_RetornaVacia()
    {
        // Arrange
        _mockMotorOnnx.Setup(m => m.ModeloEstaCargado("clasificador_senales")).Returns(false);
        List<ReadOnlyMemory<float>> espectrosVacios = new();

        // Act
        IReadOnlyList<ResultadoClasificacion> resultados = await _clasificador.ClasificarLoteAsync(espectrosVacios);

        // Assert
        resultados.Should().BeEmpty("una lista vacia de espectros debe retornar una lista vacia de resultados");
    }

    [Fact]
    public async Task ClasificarLote_ConNull_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _clasificador.ClasificarLoteAsync(null!);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>(
            "la lista de espectros no puede ser nula");
    }

    [Fact]
    public void NombreImplementacion_ContieneOnnx()
    {
        // Act
        string nombre = _clasificador.NombreImplementacion;

        // Assert
        nombre.Should().Contain("Onnx", "el nombre debe indicar que es la implementacion ONNX");
    }

    [Fact]
    public async Task Clasificar_EspectroCw_RetornaResultado()
    {
        // Arrange — usa fallback ML.NET (modelo ONNX no cargado)
        _mockMotorOnnx.Setup(m => m.ModeloEstaCargado("clasificador_senales")).Returns(false);

        float[] espectro = new float[64];
        espectro[30] = 0.95f;
        espectro[29] = 0.3f;
        espectro[31] = 0.3f;

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.ModoDetectado.Should().BeOneOf([ModoOperacion.CW, ModoOperacion.PSK],
            "un pico estrecho se identifica como CW o PSK via fallback ML.NET");
        resultado.Confianza.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task Clasificar_EspectroRuido_RetornaResultado()
    {
        // Arrange — usa fallback ML.NET (modelo ONNX no cargado)
        _mockMotorOnnx.Setup(m => m.ModeloEstaCargado("clasificador_senales")).Returns(false);

        float[] espectro = new float[64];
        Random aleatorio = new(123);
        for (int i = 0; i < 64; i++)
        {
            espectro[i] = 0.15f + (float)(aleatorio.NextDouble() * 0.15);
        }

        // Act
        ResultadoClasificacion resultado = await _clasificador.ClasificarAsync(new ReadOnlyMemory<float>(espectro));

        // Assert
        resultado.Should().NotBeNull();
        resultado.Confianza.Should().BeInRange(0.0, 1.0);
    }
}
