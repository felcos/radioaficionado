using FluentAssertions;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.ModosDigitales.Ft8;
using Serilog;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests para el decodificador FT8: parseo de mensajes, filtrado de QSOs y procesamiento de audio.
/// </summary>
public class DecodificadorFt8Tests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly ConfiguracionFt8 _configuracion;

    public DecodificadorFt8Tests()
    {
        _mockLogger = new Mock<ILogger>();
        _configuracion = new ConfiguracionFt8();
    }

    [Fact]
    public void ParsearMensaje_CQ_ExtraeIndicativoYLocalizador()
    {
        // Arrange
        string textoRaw = "CQ EA4K IN80";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.CQ);
        resultado.IndicativoEmisor.Should().Be("EA4K");
        resultado.Localizador.Should().Be("IN80");
        resultado.TextoOriginal.Should().Be(textoRaw);
    }

    [Fact]
    public void ParsearMensaje_CQ_ConModificadorDX_ExtraeIndicativoYLocalizador()
    {
        // Arrange
        string textoRaw = "CQ DX EA4K IN80";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.CQ);
        resultado.IndicativoEmisor.Should().Be("EA4K");
        resultado.Localizador.Should().Be("IN80");
    }

    [Fact]
    public void ParsearMensaje_Respuesta_ExtraeAmbosIndicativosYReporte()
    {
        // Arrange
        string textoRaw = "W1AW EA4K -09";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.Respuesta);
        resultado.IndicativoEmisor.Should().Be("EA4K");
        resultado.IndicativoReceptor.Should().Be("W1AW");
        resultado.ReporteSenal.Should().Be(-9);
        resultado.TextoOriginal.Should().Be(textoRaw);
    }

    [Fact]
    public void ParsearMensaje_Reporte_ExtraeReporteConR()
    {
        // Arrange
        string textoRaw = "EA4K W1AW R-12";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.Reporte);
        resultado.IndicativoEmisor.Should().Be("W1AW");
        resultado.IndicativoReceptor.Should().Be("EA4K");
        resultado.ReporteSenal.Should().Be(-12);
    }

    [Fact]
    public void ParsearMensaje_RRR_DetectaTipoCorrecto()
    {
        // Arrange
        string textoRaw = "W1AW EA4K RRR";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.RRR);
        resultado.IndicativoEmisor.Should().Be("EA4K");
        resultado.IndicativoReceptor.Should().Be("W1AW");
    }

    [Fact]
    public void ParsearMensaje_73_DetectaTipoCorrecto()
    {
        // Arrange
        string textoRaw = "EA4K W1AW 73";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.Setenta73);
        resultado.IndicativoEmisor.Should().Be("W1AW");
        resultado.IndicativoReceptor.Should().Be("EA4K");
    }

    [Fact]
    public void ParsearMensaje_Libre_TextoNoEstandar()
    {
        // Arrange
        string textoRaw = "HELLO WORLD FREE TEXT";

        // Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje(textoRaw);

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.Libre);
        resultado.TextoOriginal.Should().Be(textoRaw);
    }

    [Fact]
    public void DetectarMisQsos_FiltraSoloMisIndicativos()
    {
        // Arrange
        DecodificadorFt8 decodificador = new(_mockLogger.Object, _configuracion);
        List<MensajeFt8> mensajes = new()
        {
            MensajeFt8.ParsearMensaje("CQ EA4K IN80"),
            MensajeFt8.ParsearMensaje("W1AW EA4K -09"),
            MensajeFt8.ParsearMensaje("CQ DL1ABC JN49"),
            MensajeFt8.ParsearMensaje("EA4K W1AW R-12"),
        };

        // Act
        IReadOnlyList<MensajeFt8> resultado = decodificador.DetectarMisQsos("EA4K", mensajes);

        // Assert
        resultado.Should().HaveCount(3);
        resultado.Should().OnlyContain(m =>
            string.Equals(m.IndicativoEmisor, "EA4K", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.IndicativoReceptor, "EA4K", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DetectarMisQsos_SinCoincidencias_DevuelveVacio()
    {
        // Arrange
        DecodificadorFt8 decodificador = new(_mockLogger.Object, _configuracion);
        List<MensajeFt8> mensajes = new()
        {
            MensajeFt8.ParsearMensaje("CQ DL1ABC JN49"),
            MensajeFt8.ParsearMensaje("W1AW K2ABC -05"),
        };

        // Act
        IReadOnlyList<MensajeFt8> resultado = decodificador.DetectarMisQsos("EA4K", mensajes);

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcesarAudio_BufferVacio_NoDispararEvento()
    {
        // Arrange
        DecodificadorFt8 decodificador = new(_mockLogger.Object, _configuracion);
        await decodificador.IniciarAsync();
        bool eventoDisparado = false;
        decodificador.MensajeDecodificadoRecibido += (_, _) => eventoDisparado = true;

        MuestraAudio muestraVacia = new(
            ReadOnlyMemory<short>.Empty,
            12000,
            DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestraVacia);

        // Assert
        resultado.Should().BeEmpty();
        eventoDisparado.Should().BeFalse();

        decodificador.Dispose();
    }

    [Fact]
    public void ParsearMensaje_TextoVacio_DevuelveTipoLibre()
    {
        // Arrange & Act
        MensajeFt8 resultado = MensajeFt8.ParsearMensaje("");

        // Assert
        resultado.TipoMensaje.Should().Be(TipoMensajeFt8.Libre);
    }

    [Fact]
    public void GenerarCQ_FormatoCorrecto()
    {
        // Arrange & Act
        string mensaje = GeneradorMensajeFt8.GenerarCQ("EA4K", "IN80");

        // Assert
        mensaje.Should().Be("CQ EA4K IN80");
    }

    [Fact]
    public void GenerarRespuesta_FormatoCorrecto()
    {
        // Arrange & Act
        string mensaje = GeneradorMensajeFt8.GenerarRespuesta("W1AW", "EA4K", -9);

        // Assert
        mensaje.Should().Be("W1AW EA4K -09");
    }

    [Fact]
    public void GenerarReporte_FormatoCorrecto()
    {
        // Arrange & Act
        string mensaje = GeneradorMensajeFt8.GenerarReporte("W1AW", "EA4K", -12);

        // Assert
        mensaje.Should().Be("W1AW EA4K R-12");
    }

    [Fact]
    public void GenerarRRR_FormatoCorrecto()
    {
        // Arrange & Act
        string mensaje = GeneradorMensajeFt8.GenerarRRR("W1AW", "EA4K");

        // Assert
        mensaje.Should().Be("W1AW EA4K RRR");
    }

    [Fact]
    public void Generar73_FormatoCorrecto()
    {
        // Arrange & Act
        string mensaje = GeneradorMensajeFt8.Generar73("W1AW", "EA4K");

        // Assert
        mensaje.Should().Be("W1AW EA4K 73");
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorFt8 decodificador = new(_mockLogger.Object, _configuracion);

        // Act
        await decodificador.IniciarAsync();

        // Assert
        decodificador.EstaActivo.Should().BeTrue();

        decodificador.Dispose();
    }

    [Fact]
    public async Task DetenerAsync_CambiaEstadoAInactivo()
    {
        // Arrange
        DecodificadorFt8 decodificador = new(_mockLogger.Object, _configuracion);
        await decodificador.IniciarAsync();

        // Act
        await decodificador.DetenerAsync();

        // Assert
        decodificador.EstaActivo.Should().BeFalse();

        decodificador.Dispose();
    }

    [Fact]
    public void ConfiguracionFt8_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionFt8 config = new();

        // Assert
        config.FrecuenciaBase.Should().Be(14_074_000);
        config.AnchoDeVentana.Should().Be(15.0);
        config.UmbralSnr.Should().Be(-21);
        config.FrecuenciaAudioMinima.Should().Be(200);
        config.FrecuenciaAudioMaxima.Should().Be(3000);
        config.TasaDeMuestreo.Should().Be(12_000);
        config.MaximoHilosDecodificacion.Should().Be(Environment.ProcessorCount);
    }

    [Fact]
    public void Ft8Nativo_EstaDisponible_NoLanzaExcepcion()
    {
        // Arrange & Act
        bool resultado = Ft8Nativo.EstaDisponible();

        // Assert — en entorno de CI/tests, la librería nativa no estará presente
        resultado.Should().BeFalse();
    }

    [Fact]
    public void GenerarCQ_IndicativoVacio_LanzaExcepcion()
    {
        // Arrange & Act
        Action accion = () => GeneradorMensajeFt8.GenerarCQ("", "IN80");

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EsIndicativo_IndicativoValido_DevuelveTrue()
    {
        // Arrange & Act & Assert
        MensajeFt8.EsIndicativo("EA4K").Should().BeTrue();
        MensajeFt8.EsIndicativo("W1AW").Should().BeTrue();
        MensajeFt8.EsIndicativo("DL1ABC").Should().BeTrue();
        MensajeFt8.EsIndicativo("VK2ABC").Should().BeTrue();
    }

    [Fact]
    public void EsIndicativo_TextoInvalido_DevuelveFalse()
    {
        // Arrange & Act & Assert
        MensajeFt8.EsIndicativo("CQ").Should().BeFalse();
        MensajeFt8.EsIndicativo("RRR").Should().BeFalse();
        MensajeFt8.EsIndicativo("73").Should().BeFalse();
        MensajeFt8.EsIndicativo("").Should().BeFalse();
    }

    [Fact]
    public void EsLocalizador_LocalizadorValido_DevuelveTrue()
    {
        // Arrange & Act & Assert
        MensajeFt8.EsLocalizador("IN80").Should().BeTrue();
        MensajeFt8.EsLocalizador("JN49").Should().BeTrue();
        MensajeFt8.EsLocalizador("FN31").Should().BeTrue();
    }

    [Fact]
    public void EsLocalizador_TextoInvalido_DevuelveFalse()
    {
        // Arrange & Act & Assert
        MensajeFt8.EsLocalizador("EA4K").Should().BeFalse();
        MensajeFt8.EsLocalizador("ZZ99").Should().BeFalse();
        MensajeFt8.EsLocalizador("").Should().BeFalse();
    }
}
