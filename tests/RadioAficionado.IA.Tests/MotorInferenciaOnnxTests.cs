using FluentAssertions;
using RadioAficionado.Dominio.IA;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el motor de inferencia basado en ONNX Runtime.
/// Verifica la gestion del ciclo de vida de modelos, validaciones de entrada
/// y comportamiento ante condiciones de error.
/// </summary>
public sealed class MotorInferenciaOnnxTests : IDisposable
{
    private readonly MotorInferenciaOnnx _motor;

    public MotorInferenciaOnnxTests()
    {
        ConfiguracionOnnx configuracion = new();
        _motor = new MotorInferenciaOnnx(configuracion);
    }

    public void Dispose()
    {
        _motor.Dispose();
    }

    [Fact]
    public void ModeloEstaCargado_SinCargar_RetornaFalso()
    {
        // Arrange
        string nombreModelo = "modelo_inexistente";

        // Act
        bool resultado = _motor.ModeloEstaCargado(nombreModelo);

        // Assert
        resultado.Should().BeFalse("ningun modelo ha sido cargado aun");
    }

    [Fact]
    public void ObtenerModelosCargados_SinModelos_RetornaListaVacia()
    {
        // Act
        IReadOnlyList<string> modelos = _motor.ObtenerModelosCargados();

        // Assert
        modelos.Should().BeEmpty("no se han cargado modelos");
    }

    [Fact]
    public async Task CargarModeloAsync_RutaInvalida_LanzaExcepcion()
    {
        // Arrange
        string rutaInvalida = @"C:\ruta\que\no\existe\modelo.onnx";
        string nombre = "modelo_test";

        // Act
        Func<Task> accion = () => _motor.CargarModeloAsync(rutaInvalida, nombre, CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<FileNotFoundException>(
            "el archivo de modelo no existe en la ruta especificada");
    }

    [Fact]
    public async Task CargarModeloAsync_RutaNula_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _motor.CargarModeloAsync(null!, "modelo_test", CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>(
            "la ruta del modelo no puede ser nula");
    }

    [Fact]
    public async Task CargarModeloAsync_NombreNulo_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _motor.CargarModeloAsync(@"C:\modelo.onnx", null!, CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>(
            "el nombre del modelo no puede ser nulo");
    }

    [Fact]
    public void DescargarModelo_SinCargar_NoLanzaExcepcion()
    {
        // Act
        Action accion = () => _motor.DescargarModelo("modelo_inexistente");

        // Assert
        accion.Should().NotThrow("descargar un modelo inexistente no debe lanzar excepcion");
    }

    [Fact]
    public void DescargarModelo_ModeloInexistente_NoLanzaExcepcion()
    {
        // Arrange
        string nombreInexistente = "modelo_que_nunca_se_cargo";

        // Act
        Action accion = () => _motor.DescargarModelo(nombreInexistente);

        // Assert
        accion.Should().NotThrow("descargar un modelo que no existe no debe causar error");
    }

    [Fact]
    public async Task EjecutarInferenciaAsync_SinModelo_LanzaExcepcion()
    {
        // Arrange
        float[] entrada = [1.0f, 2.0f, 3.0f];

        // Act
        Func<Task> accion = () => _motor.EjecutarInferenciaAsync(entrada, "modelo_no_cargado", CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>(
            "no se puede ejecutar inferencia sin cargar el modelo primero");
    }

    [Fact]
    public async Task EjecutarInferenciaAsync_EntradaNula_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _motor.EjecutarInferenciaAsync(null!, "modelo_test", CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>(
            "la entrada no puede ser nula");
    }

    [Fact]
    public async Task EjecutarInferenciaLoteAsync_ListaVacia_RetornaListaVacia()
    {
        // Arrange
        List<float[]> entradasVacias = new();

        // Act
        IReadOnlyList<ResultadoInferencia> resultados = await _motor.EjecutarInferenciaLoteAsync(
            entradasVacias, "modelo_test", CancellationToken.None);

        // Assert
        resultados.Should().BeEmpty("una lista de entradas vacia debe producir una lista de resultados vacia");
    }

    [Fact]
    public async Task EjecutarInferenciaLoteAsync_ConNull_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _motor.EjecutarInferenciaLoteAsync(null!, "modelo_test", CancellationToken.None);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>(
            "la lista de entradas no puede ser nula");
    }

    [Fact]
    public void Dispose_MultiplesVeces_NoLanzaExcepcion()
    {
        // Arrange
        ConfiguracionOnnx configuracion = new();
        MotorInferenciaOnnx motorLocal = new(configuracion);

        // Act
        Action accion = () =>
        {
            motorLocal.Dispose();
            motorLocal.Dispose();
            motorLocal.Dispose();
        };

        // Assert
        accion.Should().NotThrow("Dispose debe ser idempotente y no lanzar excepcion al llamarse multiples veces");
    }
}
