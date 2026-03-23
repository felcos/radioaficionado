using FluentAssertions;
using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Activaciones;

/// <summary>
/// Tests unitarios para la entidad <see cref="Activacion"/>.
/// Valida la creación, transiciones de estado, adición de QSOs y validaciones de negocio.
/// </summary>
public class ActivacionTests
{
    [Fact]
    public void Crear_DatosValidosPota_CreaActivacionCorrectamente()
    {
        // Arrange
        TipoActivacion tipo = TipoActivacion.Pota;
        string referencia = "US-0001";
        Indicativo indicativo = new Indicativo("EA4ABC");

        // Act
        Activacion activacion = Activacion.Crear(tipo, referencia, indicativo);

        // Assert
        activacion.Id.Should().NotBeEmpty();
        activacion.TipoActivacion.Should().Be(TipoActivacion.Pota);
        activacion.Referencia.Should().Be("US-0001");
        activacion.IndicativoActivador.Valor.Should().Be("EA4ABC");
        activacion.EstadoActivacion.Should().Be(EstadoActivacion.Planificada);
        activacion.FechaFin.Should().BeNull();
        activacion.Qsos.Should().BeEmpty();
    }

    [Fact]
    public void Crear_DatosValidosSota_CreaActivacionCorrectamente()
    {
        // Arrange
        TipoActivacion tipo = TipoActivacion.Sota;
        string referencia = "EA4/MD-001";
        Indicativo indicativo = new Indicativo("EA4ABC");
        Localizador localizador = new Localizador("IN80");

        // Act
        Activacion activacion = Activacion.Crear(tipo, referencia, indicativo, localizador, "Activación de prueba");

        // Assert
        activacion.TipoActivacion.Should().Be(TipoActivacion.Sota);
        activacion.Referencia.Should().Be("EA4/MD-001");
        activacion.Localizador.Should().NotBeNull();
        activacion.Localizador!.Value.Valor.Should().Be("IN80");
        activacion.Notas.Should().Be("Activación de prueba");
    }

    [Fact]
    public void Crear_ReferenciaPotaInvalida_LanzaArgumentException()
    {
        // Arrange & Act
        Action accion = () => Activacion.Crear(
            TipoActivacion.Pota,
            "INVALIDO",
            new Indicativo("EA4ABC"));

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ReferenciaSotaInvalida_LanzaArgumentException()
    {
        // Arrange & Act
        Action accion = () => Activacion.Crear(
            TipoActivacion.Sota,
            "INVALIDO",
            new Indicativo("EA4ABC"));

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_ReferenciaVacia_LanzaArgumentException()
    {
        // Arrange & Act
        Action accion = () => Activacion.Crear(
            TipoActivacion.Pota,
            "",
            new Indicativo("EA4ABC"));

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IniciarActivacion_EstadoPlanificada_CambiaAEnCurso()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));

        // Act
        activacion.IniciarActivacion();

        // Assert
        activacion.EstadoActivacion.Should().Be(EstadoActivacion.EnCurso);
        activacion.FechaModificacion.Should().NotBeNull();
    }

    [Fact]
    public void IniciarActivacion_EstadoEnCurso_LanzaInvalidOperationException()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();

        // Act
        Action accion = () => activacion.IniciarActivacion();

        // Assert
        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompletarActivacion_EstadoEnCurso_CambiaACompletada()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();

        // Act
        activacion.CompletarActivacion();

        // Assert
        activacion.EstadoActivacion.Should().Be(EstadoActivacion.Completada);
        activacion.FechaFin.Should().NotBeNull();
    }

    [Fact]
    public void CompletarActivacion_EstadoPlanificada_LanzaInvalidOperationException()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));

        // Act
        Action accion = () => activacion.CompletarActivacion();

        // Assert
        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AgregarQso_ActivacionEnCurso_AgregaCorrectamente()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();

        Qso qso = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");

        // Act
        activacion.AgregarQso(qso);

        // Assert
        activacion.Qsos.Should().HaveCount(1);
        activacion.Qsos[0].Id.Should().Be(qso.Id);
    }

    [Fact]
    public void AgregarQso_ActivacionPlanificada_LanzaInvalidOperationException()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));

        Qso qso = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");

        // Act
        Action accion = () => activacion.AgregarQso(qso);

        // Assert
        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AgregarQso_QsoNulo_LanzaArgumentNullException()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();

        // Act
        Action accion = () => activacion.AgregarQso(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CancelarActivacion_EstadoEnCurso_CambiaACancelada()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();

        // Act
        activacion.CancelarActivacion();

        // Assert
        activacion.EstadoActivacion.Should().Be(EstadoActivacion.Cancelada);
        activacion.FechaFin.Should().NotBeNull();
    }

    [Fact]
    public void CancelarActivacion_EstadoCompletada_LanzaInvalidOperationException()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Pota,
            "US-0001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();
        activacion.CompletarActivacion();

        // Act
        Action accion = () => activacion.CancelarActivacion();

        // Assert
        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AgregarMultiplesQsos_ActivacionEnCurso_AgregaTodosCorrectamente()
    {
        // Arrange
        Activacion activacion = Activacion.Crear(
            TipoActivacion.Sota,
            "EA4/MD-001",
            new Indicativo("EA4ABC"));
        activacion.IniciarActivacion();

        Qso qso1 = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            DateTimeOffset.UtcNow.AddMinutes(-10),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");

        Qso qso2 = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("DL1ABC"),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            Frecuencia.DesdeMHz(7.074),
            ModoOperacion.FT8,
            "59");

        // Act
        activacion.AgregarQso(qso1);
        activacion.AgregarQso(qso2);

        // Assert
        activacion.Qsos.Should().HaveCount(2);
    }
}
