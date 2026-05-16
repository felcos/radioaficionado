using FluentAssertions;
using RadioAficionado.Dominio.IA;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el servicio de entrenamiento y exportacion de modelos ONNX.
/// Verifica validaciones de parametros, entrenamiento real y generacion de metricas.
/// </summary>
public sealed class EntrenadorModelosIaTests : IDisposable
{
    private readonly EntrenadorModelosIa _entrenador = new();
    private readonly string _directorioTemporal;

    public EntrenadorModelosIaTests()
    {
        _directorioTemporal = Path.Combine(Path.GetTempPath(), $"RadioAficionadoTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directorioTemporal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directorioTemporal))
        {
            Directory.Delete(_directorioTemporal, recursive: true);
        }
    }

    [Fact]
    public async Task EntrenarClasificador_RutaNula_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _entrenador.EntrenarYExportarClasificadorAsync(null!, CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EntrenarClasificador_RutaVacia_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _entrenador.EntrenarYExportarClasificadorAsync(string.Empty, CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EntrenarAnalizador_RutaNula_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _entrenador.EntrenarYExportarAnalizadorAsync(null!, CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EntrenarAnalizador_RutaVacia_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _entrenador.EntrenarYExportarAnalizadorAsync("   ", CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EntrenarTodos_DirectorioNulo_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _entrenador.EntrenarTodosLosModelosAsync(null!, CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EntrenarClasificador_Completo_RetornaMetricas()
    {
        // Arrange
        string rutaSalida = Path.Combine(_directorioTemporal, "clasificador_test.onnx");

        // Act
        MetricasClasificacion metricas = await _entrenador.EntrenarYExportarClasificadorAsync(
            rutaSalida, CancellationToken.None);

        // Assert
        metricas.Should().NotBeNull();
        metricas.Precision.Should().BeGreaterThan(0, "el modelo debe tener accuracy mayor que cero");
        metricas.TiempoEntrenamiento.Should().BeGreaterThan(TimeSpan.Zero);
        metricas.MatrizConfusion.Should().NotBeNullOrWhiteSpace();
        File.Exists(rutaSalida).Should().BeTrue("el archivo ONNX debe haberse creado");
    }

    [Fact]
    public async Task EntrenarAnalizador_Completo_RetornaMetricas()
    {
        // Arrange
        string rutaSalida = Path.Combine(_directorioTemporal, "analizador_test.onnx");

        // Act
        MetricasRegresion metricas = await _entrenador.EntrenarYExportarAnalizadorAsync(
            rutaSalida, CancellationToken.None);

        // Assert
        metricas.Should().NotBeNull();
        metricas.RCuadrado.Should().BeGreaterThan(0, "el modelo debe tener R² mayor que cero");
        metricas.TiempoEntrenamiento.Should().BeGreaterThan(TimeSpan.Zero);
        File.Exists(rutaSalida).Should().BeTrue("el archivo ONNX debe haberse creado");
    }

    [Fact]
    public async Task MetricasClasificacion_PrecisionEnRangoValido()
    {
        // Arrange
        string rutaSalida = Path.Combine(_directorioTemporal, "clasificador_rango.onnx");

        // Act
        MetricasClasificacion metricas = await _entrenador.EntrenarYExportarClasificadorAsync(
            rutaSalida, CancellationToken.None);

        // Assert
        metricas.Precision.Should().BeInRange(0.0, 1.0,
            "la precision debe estar entre 0.0 y 1.0");
        metricas.PerdidaLog.Should().BeGreaterThanOrEqualTo(0,
            "la perdida log no puede ser negativa");
    }

    [Fact]
    public async Task MetricasRegresion_RCuadradoEnRangoValido()
    {
        // Arrange
        string rutaSalida = Path.Combine(_directorioTemporal, "analizador_rango.onnx");

        // Act
        MetricasRegresion metricas = await _entrenador.EntrenarYExportarAnalizadorAsync(
            rutaSalida, CancellationToken.None);

        // Assert
        metricas.RCuadrado.Should().BeLessThanOrEqualTo(1.0,
            "R² no puede ser mayor que 1.0");
        metricas.ErrorCuadraticoMedio.Should().BeGreaterThanOrEqualTo(0,
            "RMSE no puede ser negativo");
        metricas.ErrorAbsolutoMedio.Should().BeGreaterThanOrEqualTo(0,
            "MAE no puede ser negativo");
    }

    [Fact]
    public async Task EntrenarTodos_CreaArchivosOnnx()
    {
        // Arrange
        string directorio = Path.Combine(_directorioTemporal, "todos");

        // Act
        await _entrenador.EntrenarTodosLosModelosAsync(directorio, CancellationToken.None);

        // Assert
        File.Exists(Path.Combine(directorio, "clasificador_senales.onnx")).Should().BeTrue(
            "debe crearse el archivo del clasificador de senales");
        File.Exists(Path.Combine(directorio, "analizador_propagacion.onnx")).Should().BeTrue(
            "debe crearse el archivo del analizador de propagacion");
    }
}
