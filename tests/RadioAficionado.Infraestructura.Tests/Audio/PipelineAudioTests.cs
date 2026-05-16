using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.Audio;

namespace RadioAficionado.Infraestructura.Tests.Audio;

/// <summary>
/// Tests unitarios para <see cref="PipelineAudioNAudio"/>.
/// Verifica el comportamiento inicial y la gestión de recursos sin requerir hardware de audio.
/// </summary>
public class PipelineAudioTests
{
    [Fact]
    public void Constructor_NoLanzaExcepcion()
    {
        // Arrange & Act
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void EstaActivo_Inicial_EsFalso()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();

        // Act
        bool estaActivo = pipeline.EstaActivo;

        // Assert
        estaActivo.Should().BeFalse();
    }

    [Fact]
    public void TasaDeMuestreoHz_Inicial_EsCero()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();

        // Act
        int tasa = pipeline.TasaDeMuestreoHz;

        // Assert
        tasa.Should().Be(0);
    }

    [Fact]
    public void Suscribir_ConsumidorValido_RetornaGuidNoVacio()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();
        Action<MuestraAudio> consumidor = _ => { };

        // Act
        Guid id = pipeline.Suscribir(consumidor);

        // Assert
        id.Should().NotBeEmpty();
    }

    [Fact]
    public void Suscribir_ConsumidorNulo_LanzaArgumentNullException()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();

        // Act
        Action accion = () => pipeline.Suscribir(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Desuscribir_IdInexistente_NoLanzaExcepcion()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();
        Guid idInexistente = Guid.NewGuid();

        // Act
        Action accion = () => pipeline.Desuscribir(idInexistente);

        // Assert
        accion.Should().NotThrow();
    }

    [Fact]
    public void Suscribir_MultiplesConsumidores_RetornaIdsDistintos()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();
        Action<MuestraAudio> consumidor1 = _ => { };
        Action<MuestraAudio> consumidor2 = _ => { };

        // Act
        Guid id1 = pipeline.Suscribir(consumidor1);
        Guid id2 = pipeline.Suscribir(consumidor2);

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task DisposeAsync_SinCaptura_NoLanzaExcepcion()
    {
        // Arrange
        PipelineAudioNAudio pipeline = new PipelineAudioNAudio();

        // Act
        Func<Task> accion = async () => await pipeline.DisposeAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }
}
