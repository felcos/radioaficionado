using System.Reflection;
using FluentAssertions;
using RadioAficionado.IA;

namespace RadioAficionado.IA.Tests;

/// <summary>
/// Tests para el exportador de modelos ML.NET a formato ONNX.
/// Verifica validaciones de parametros y estructura publica de la clase.
/// </summary>
public sealed class ExportadorModeloOnnxTests
{
    [Fact]
    public void ExportarClasificador_RutaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => ExportadorModeloOnnx.ExportarClasificador(null!);

        // Assert
        accion.Should().Throw<ArgumentException>(
            "la ruta de salida no puede ser nula");
    }

    [Fact]
    public void ExportarAnalizador_RutaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => ExportadorModeloOnnx.ExportarAnalizador(null!);

        // Assert
        accion.Should().Throw<ArgumentException>(
            "la ruta de salida no puede ser nula");
    }

    [Fact]
    public void ExportarClasificador_RutaVacia_LanzaExcepcion()
    {
        // Act
        Action accion = () => ExportadorModeloOnnx.ExportarClasificador(string.Empty);

        // Assert
        accion.Should().Throw<ArgumentException>(
            "la ruta de salida no puede estar vacia");
    }

    [Fact]
    public void ExportarAnalizador_RutaVacia_LanzaExcepcion()
    {
        // Act
        Action accion = () => ExportadorModeloOnnx.ExportarAnalizador(string.Empty);

        // Assert
        accion.Should().Throw<ArgumentException>(
            "la ruta de salida no puede estar vacia");
    }

    [Fact]
    public void Clase_TieneMetodosPublicos_Estaticos()
    {
        // Arrange
        Type tipo = typeof(ExportadorModeloOnnx);

        // Act
        MethodInfo[] metodosPublicosEstaticos = tipo.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        // Assert
        metodosPublicosEstaticos.Should().HaveCountGreaterThanOrEqualTo(3,
            "la clase debe tener al menos 3 metodos publicos estaticos: ExportarModeloMlNet, ExportarClasificador y ExportarAnalizador");

        IEnumerable<string> nombres = metodosPublicosEstaticos.Select(m => m.Name);
        nombres.Should().Contain("ExportarModeloMlNet");
        nombres.Should().Contain("ExportarClasificador");
        nombres.Should().Contain("ExportarAnalizador");
    }
}
