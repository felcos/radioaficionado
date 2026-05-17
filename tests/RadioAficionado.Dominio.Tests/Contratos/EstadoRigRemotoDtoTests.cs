using FluentAssertions;
using RadioAficionado.Compartido.Contratos;

namespace RadioAficionado.Dominio.Tests.Contratos;

/// <summary>
/// Tests unitarios para el record <see cref="EstadoRigRemotoDto"/>.
/// </summary>
public class EstadoRigRemotoDtoTests
{
    [Fact]
    public void Constructor_ConDatos_PropiedadesCorrectas()
    {
        // Arrange & Act
        EstadoRigRemotoDto estado = new(
            FrecuenciaHz: 14074000,
            FrecuenciaDisplay: "14.074.000 Hz",
            Modo: "FT8",
            Banda: "20m",
            NivelSenal: -12,
            Transmitiendo: false,
            VfoActivo: 'A',
            PotenciaVatios: 50.0,
            SplitActivo: false,
            Conectado: true);

        // Assert
        estado.FrecuenciaHz.Should().Be(14074000);
        estado.FrecuenciaDisplay.Should().Be("14.074.000 Hz");
        estado.Modo.Should().Be("FT8");
        estado.Banda.Should().Be("20m");
        estado.NivelSenal.Should().Be(-12);
        estado.Transmitiendo.Should().BeFalse();
        estado.VfoActivo.Should().Be('A');
        estado.PotenciaVatios.Should().Be(50.0);
        estado.SplitActivo.Should().BeFalse();
        estado.Conectado.Should().BeTrue();
    }

    [Fact]
    public void Record_Igualdad_MismosDatos()
    {
        // Arrange
        EstadoRigRemotoDto estado1 = new(
            FrecuenciaHz: 7074000,
            FrecuenciaDisplay: "7.074.000 Hz",
            Modo: "FT8",
            Banda: "40m",
            NivelSenal: -6,
            Transmitiendo: true,
            VfoActivo: 'B',
            PotenciaVatios: 100.0,
            SplitActivo: true,
            Conectado: true);

        EstadoRigRemotoDto estado2 = new(
            FrecuenciaHz: 7074000,
            FrecuenciaDisplay: "7.074.000 Hz",
            Modo: "FT8",
            Banda: "40m",
            NivelSenal: -6,
            Transmitiendo: true,
            VfoActivo: 'B',
            PotenciaVatios: 100.0,
            SplitActivo: true,
            Conectado: true);

        // Act & Assert
        estado1.Should().Be(estado2);
    }
}
