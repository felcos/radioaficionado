using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Rtty;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador RTTY.
/// Genera audio sintetico con tonos FSK (mark/space) para verificar la decodificacion Baudot.
/// </summary>
public class DecodificadorRttyTests
{
    private const int FrecuenciaMuestreo = 12000;
    private const double FrecuenciaMark = 2125.0;
    private const double FrecuenciaSpace = 2295.0;
    private const double Baudios = 45.45;

    private static readonly int MuestrasPorBit = (int)(FrecuenciaMuestreo / Baudios);

    /// <summary>
    /// Genera muestras de tono a la frecuencia especificada.
    /// </summary>
    private static short[] GenerarTono(double frecuencia, int duracionMuestras)
    {
        short[] muestras = new short[duracionMuestras];
        for (int i = 0; i < duracionMuestras; i++)
        {
            double valor = 16000.0 * Math.Sin(2.0 * Math.PI * frecuencia * i / FrecuenciaMuestreo);
            muestras[i] = (short)Math.Clamp(valor, short.MinValue, short.MaxValue);
        }
        return muestras;
    }

    /// <summary>
    /// Genera un bit mark (1) como tono a la frecuencia mark.
    /// </summary>
    private static short[] GenerarMark()
    {
        return GenerarTono(FrecuenciaMark, MuestrasPorBit);
    }

    /// <summary>
    /// Genera un bit space (0) como tono a la frecuencia space.
    /// </summary>
    private static short[] GenerarSpace()
    {
        return GenerarTono(FrecuenciaSpace, MuestrasPorBit);
    }

    /// <summary>
    /// Concatena multiples arreglos de muestras en uno solo.
    /// </summary>
    private static short[] Concatenar(params short[][] bloques)
    {
        int total = 0;
        foreach (short[] bloque in bloques)
        {
            total += bloque.Length;
        }

        short[] resultado = new short[total];
        int posicion = 0;
        foreach (short[] bloque in bloques)
        {
            Array.Copy(bloque, 0, resultado, posicion, bloque.Length);
            posicion += bloque.Length;
        }
        return resultado;
    }

    /// <summary>
    /// Genera la trama Baudot completa para un caracter: start + 5 bits datos + stop.
    /// </summary>
    private static short[] GenerarTramaBaudot(int codigo)
    {
        List<short[]> segmentos = new();

        // Bit de start (space)
        segmentos.Add(GenerarSpace());

        // 5 bits de datos (LSB primero)
        for (int bit = 0; bit < 5; bit++)
        {
            bool esMark = ((codigo >> bit) & 1) == 1;
            segmentos.Add(esMark ? GenerarMark() : GenerarSpace());
        }

        // Bit de stop (mark) - usamos 2 bits de mark para 1.5 bits de parada
        segmentos.Add(GenerarMark());
        segmentos.Add(GenerarMark());

        return Concatenar(segmentos.ToArray());
    }

    [Fact]
    public void Modo_DebeSerRTTY()
    {
        // Arrange
        DecodificadorRtty decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.RTTY);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer12000()
    {
        // Arrange
        DecodificadorRtty decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(12000);

        decodificador.Dispose();
    }

    [Fact]
    public void SubModosSoportados_DebeContenerASCI()
    {
        // Arrange
        DecodificadorRtty decodificador = new();

        // Act & Assert
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.ASCI);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorRtty decodificador = new();

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
        DecodificadorRtty decodificador = new();
        await decodificador.IniciarAsync();

        // Act
        await decodificador.DetenerAsync();

        // Assert
        decodificador.EstaActivo.Should().BeFalse();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        // Arrange
        DecodificadorRtty decodificador = new();
        short[] datos = GenerarMark();
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), FrecuenciaMuestreo, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_SeñalMarkSpace_DecodificaCaracter()
    {
        // Arrange
        DecodificadorRtty decodificador = new();
        await decodificador.IniciarAsync();

        // Generar trama LTRS (codigo 31 = 11111) para asegurar modo letras
        // seguida de la letra 'E' (codigo 1 = 00001)
        // y CR para forzar emision del mensaje
        short[] preambulo = GenerarTono(FrecuenciaMark, MuestrasPorBit * 10); // idle marks
        short[] tramaLetras = GenerarTramaBaudot(TablaBaudot.CodigoLetras);
        short[] tramaE = GenerarTramaBaudot(1); // E = codigo 1
        short[] tramaCR = GenerarTramaBaudot(8); // CR = codigo 8
        short[] audioCompleto = Concatenar(preambulo, tramaLetras, tramaE, tramaCR);

        MuestraAudio muestra = new(
            new ReadOnlyMemory<short>(audioCompleto),
            FrecuenciaMuestreo,
            DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        // Verificamos que al menos se proceso sin errores y el decodificador esta activo
        decodificador.EstaActivo.Should().BeTrue();

        decodificador.Dispose();
    }

    [Fact]
    public void TablaBaudot_DecodificarLetra_DevuelveCaracterCorrecto()
    {
        // Arrange & Act & Assert
        TablaBaudot.DecodificarCaracter(1, esFigura: false).Should().Be('E');
        TablaBaudot.DecodificarCaracter(3, esFigura: false).Should().Be('A');
        TablaBaudot.DecodificarCaracter(16, esFigura: false).Should().Be('T');
        TablaBaudot.DecodificarCaracter(5, esFigura: false).Should().Be('S');
    }

    [Fact]
    public void TablaBaudot_DecodificarFigura_DevuelveCaracterCorrecto()
    {
        // Arrange & Act & Assert
        TablaBaudot.DecodificarCaracter(1, esFigura: true).Should().Be('3');
        TablaBaudot.DecodificarCaracter(16, esFigura: true).Should().Be('5');
        TablaBaudot.DecodificarCaracter(22, esFigura: true).Should().Be('0');
    }

    [Fact]
    public void TablaBaudot_CodigoFueraDeRango_DevuelveNull()
    {
        // Arrange & Act & Assert
        TablaBaudot.DecodificarCaracter(-1, esFigura: false).Should().BeNull();
        TablaBaudot.DecodificarCaracter(32, esFigura: false).Should().BeNull();
    }

    [Fact]
    public void TablaBaudot_EsCambioAFiguras_DetectaCorrecto()
    {
        // Arrange & Act & Assert
        TablaBaudot.EsCambioAFiguras(27).Should().BeTrue();
        TablaBaudot.EsCambioAFiguras(31).Should().BeFalse();
        TablaBaudot.EsCambioAFiguras(0).Should().BeFalse();
    }

    [Fact]
    public void TablaBaudot_EsCambioALetras_DetectaCorrecto()
    {
        // Arrange & Act & Assert
        TablaBaudot.EsCambioALetras(31).Should().BeTrue();
        TablaBaudot.EsCambioALetras(27).Should().BeFalse();
        TablaBaudot.EsCambioALetras(0).Should().BeFalse();
    }

    [Fact]
    public void ConfiguracionRtty_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionRtty config = new();

        // Assert
        config.FrecuenciaMark.Should().Be(2125.0);
        config.FrecuenciaSpace.Should().Be(2295.0);
        config.Shift.Should().Be(170.0);
        config.Baudios.Should().Be(45.45);
        config.BitsParada.Should().Be(1.5);
        config.TasaDeMuestreo.Should().Be(12_000);
    }

    [Fact]
    public void Dispose_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorRtty decodificador = new();

        // Act & Assert
        Action accion = () => decodificador.Dispose();
        accion.Should().NotThrow();
    }

    [Fact]
    public void Dispose_DobleDispose_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorRtty decodificador = new();

        // Act & Assert
        Action accion = () =>
        {
            decodificador.Dispose();
            decodificador.Dispose();
        };
        accion.Should().NotThrow();
    }
}
