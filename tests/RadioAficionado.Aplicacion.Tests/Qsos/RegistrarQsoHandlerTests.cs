using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Aplicacion.Qsos.RegistrarQso;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Aplicacion.Tests.Qsos;

/// <summary>
/// Tests unitarios para <see cref="RegistrarQsoHandler"/>.
/// </summary>
public class RegistrarQsoHandlerTests
{
    private readonly Mock<IRepositorioQso> _repositorioQsoMock;
    private readonly Mock<IUnidadDeTrabajo> _unidadDeTrabajoMock;
    private readonly Mock<ILogger<RegistrarQsoHandler>> _loggerMock;
    private readonly RegistrarQsoHandler _handler;

    /// <summary>
    /// Inicializa los mocks y el handler para cada test.
    /// </summary>
    public RegistrarQsoHandlerTests()
    {
        _repositorioQsoMock = new Mock<IRepositorioQso>();
        _unidadDeTrabajoMock = new Mock<IUnidadDeTrabajo>();
        _loggerMock = new Mock<ILogger<RegistrarQsoHandler>>();

        _handler = new RegistrarQsoHandler(
            _repositorioQsoMock.Object,
            _unidadDeTrabajoMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Crea un comando válido con valores por defecto para reutilizar en tests.
    /// </summary>
    private static RegistrarQsoComando CrearComandoValido(
        string? indicativoPropio = null,
        string? indicativoContacto = null,
        long? frecuenciaHz = null,
        ModoOperacion? modo = null,
        string? senalEnviada = null,
        string? senalRecibida = null,
        double? potencia = null,
        string? localizadorContacto = null,
        string? notas = null)
    {
        return new RegistrarQsoComando
        {
            IndicativoPropio = indicativoPropio ?? "EA4ABC",
            IndicativoContacto = indicativoContacto ?? "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = frecuenciaHz ?? 14_074_000,
            Modo = modo ?? ModoOperacion.FT8,
            SenalEnviada = senalEnviada ?? "59",
            SenalRecibida = senalRecibida ?? string.Empty,
            Potencia = potencia,
            LocalizadorContacto = localizadorContacto,
            Notas = notas
        };
    }

    /// <summary>
    /// Verifica que un comando válido llama a AgregarAsync y GuardarCambiosAsync.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoValido_CreaYGuardaQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido();

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        _repositorioQsoMock.Verify(
            r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unidadDeTrabajoMock.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifica que el resultado contiene un Id de QSO válido (no vacío).
    /// </summary>
    [Fact]
    public async Task Handle_ComandoValido_RetornaIdDelQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido();

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.QsoId.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Verifica que el indicativo propio del comando se asigna correctamente al QSO creado.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConIndicativoPropio_LoAsignaAlQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(indicativoPropio: "EA4ABC");
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.IndicativoPropio.Valor.Should().Be("EA4ABC");
    }

    /// <summary>
    /// Verifica que la frecuencia del comando se asigna correctamente al QSO creado.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConFrecuencia_LoAsignaAlQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(frecuenciaHz: 7_074_000);
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.Frecuencia.Hz.Should().Be(7_074_000);
    }

    /// <summary>
    /// Verifica que el modo de operación del comando se asigna correctamente al QSO creado.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConModo_LoAsignaAlQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(modo: ModoOperacion.CW);
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.Modo.Should().Be(ModoOperacion.CW);
    }

    /// <summary>
    /// Verifica que el localizador del contacto se asigna correctamente al QSO creado.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConLocalizador_LoAsignaAlQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(localizadorContacto: "IO91WM");
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.LocalizadorContacto.Should().NotBeNull();
        qsoCapturado.LocalizadorContacto!.Value.Valor.Should().Be("IO91WM");
    }

    /// <summary>
    /// Verifica que si el comando tiene señal recibida, el QSO se completa automáticamente.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConSenalRecibida_CompletaElQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(senalRecibida: "59");
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.SenalRecibida.Should().Be("59");
        qsoCapturado.FechaHoraFin.Should().NotBeNull();
    }

    /// <summary>
    /// Verifica que si la señal recibida está vacía, el QSO no se completa.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoSinSenalRecibida_NoCompletaElQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(senalRecibida: string.Empty);
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.FechaHoraFin.Should().BeNull();
    }

    /// <summary>
    /// Verifica que un indicativo inválido devuelve un resultado fallido en vez de lanzar excepción.
    /// </summary>
    [Fact]
    public async Task Handle_IndicativoInvalido_RetornaResultadoFallido()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(indicativoContacto: "XX");

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Error.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifica que la potencia se asigna correctamente al QSO creado.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConPotencia_LoAsignaAlQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(potencia: 100.0);
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.Potencia.Should().Be(100.0);
    }

    /// <summary>
    /// Verifica que las notas se asignan correctamente al QSO creado.
    /// </summary>
    [Fact]
    public async Task Handle_ComandoConNotas_LoAsignaAlQso()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido(notas: "Primer contacto con W1AW");
        Qso? qsoCapturado = null;

        _repositorioQsoMock
            .Setup(r => r.AgregarAsync(It.IsAny<Qso>(), It.IsAny<CancellationToken>()))
            .Callback<Qso, CancellationToken>((qso, _) => qsoCapturado = qso);

        // Act
        RegistrarQsoResultado resultado = await _handler.Handle(comando, CancellationToken.None);

        // Assert
        qsoCapturado.Should().NotBeNull();
        qsoCapturado!.Notas.Should().Be("Primer contacto con W1AW");
    }
}
