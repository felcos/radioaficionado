using FluentAssertions;
using FluentValidation.Results;
using RadioAficionado.Aplicacion.Qsos.RegistrarQso;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Aplicacion.Tests.Qsos;

/// <summary>
/// Tests unitarios para <see cref="RegistrarQsoValidador"/>.
/// </summary>
public class RegistrarQsoValidadorTests
{
    private readonly RegistrarQsoValidador _validador;

    /// <summary>
    /// Inicializa el validador para cada test.
    /// </summary>
    public RegistrarQsoValidadorTests()
    {
        _validador = new RegistrarQsoValidador();
    }

    /// <summary>
    /// Crea un comando válido con valores por defecto para reutilizar en tests.
    /// </summary>
    private static RegistrarQsoComando CrearComandoValido()
    {
        return new RegistrarQsoComando
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59",
            SenalRecibida = string.Empty,
            Potencia = null,
            LocalizadorContacto = null,
            Notas = null
        };
    }

    /// <summary>
    /// Verifica que un comando con todos los campos válidos no presenta errores.
    /// </summary>
    [Fact]
    public void Validar_ComandoValido_NoPresentaErrores()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido();

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que un comando sin indicativo de contacto presenta error.
    /// </summary>
    [Fact]
    public void Validar_SinIndicativoContacto_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = string.Empty,
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "IndicativoContacto");
    }

    /// <summary>
    /// Verifica que un comando sin indicativo propio presenta error.
    /// </summary>
    [Fact]
    public void Validar_SinIndicativoPropio_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = string.Empty,
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "IndicativoPropio");
    }

    /// <summary>
    /// Verifica que una frecuencia cero o negativa presenta error.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validar_FrecuenciaNoPositiva_PresentaError(long frecuenciaHz)
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = frecuenciaHz,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "FrecuenciaHz");
    }

    /// <summary>
    /// Verifica que un comando sin señal enviada presenta error.
    /// </summary>
    [Fact]
    public void Validar_SinSenalEnviada_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = string.Empty
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "SenalEnviada");
    }

    /// <summary>
    /// Verifica que una fecha de inicio futura presenta error.
    /// </summary>
    [Fact]
    public void Validar_FechaInicioFutura_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddHours(1),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "FechaHoraInicio");
    }

    /// <summary>
    /// Verifica que un modo de operación inválido (fuera del enum) presenta error.
    /// </summary>
    [Fact]
    public void Validar_ModoInvalido_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = (ModoOperacion)999,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Modo");
    }

    /// <summary>
    /// Verifica que una potencia negativa presenta error.
    /// </summary>
    [Fact]
    public void Validar_PotenciaNegativa_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59",
            Potencia = -10.0
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "Potencia");
    }

    /// <summary>
    /// Verifica que una potencia nula (no proporcionada) no presenta error.
    /// </summary>
    [Fact]
    public void Validar_PotenciaNula_NoPresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = CrearComandoValido();

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un localizador con formato inválido presenta error.
    /// </summary>
    [Theory]
    [InlineData("ZZZZZ")]
    [InlineData("12AB")]
    [InlineData("IO91WM35XX")]
    public void Validar_LocalizadorInvalido_PresentaError(string localizador)
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59",
            LocalizadorContacto = localizador
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "LocalizadorContacto");
    }

    /// <summary>
    /// Verifica que un localizador válido no presenta error.
    /// </summary>
    [Theory]
    [InlineData("IO91")]
    [InlineData("IO91WM")]
    [InlineData("IO91WM35")]
    public void Validar_LocalizadorValido_NoPresentaError(string localizador)
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59",
            LocalizadorContacto = localizador
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un indicativo demasiado corto presenta error.
    /// </summary>
    [Fact]
    public void Validar_IndicativoContactoMuyCorto_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABC",
            IndicativoContacto = "AB",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "IndicativoContacto");
    }

    /// <summary>
    /// Verifica que un indicativo demasiado largo presenta error.
    /// </summary>
    [Fact]
    public void Validar_IndicativoPropioMuyLargo_PresentaError()
    {
        // Arrange
        RegistrarQsoComando comando = new()
        {
            IndicativoPropio = "EA4ABCDEFGH",
            IndicativoContacto = "W1AW",
            FechaHoraInicio = DateTimeOffset.UtcNow.AddMinutes(-5),
            FrecuenciaHz = 14_074_000,
            Modo = ModoOperacion.FT8,
            SenalEnviada = "59"
        };

        // Act
        ValidationResult resultado = _validador.Validate(comando);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == "IndicativoPropio");
    }
}
