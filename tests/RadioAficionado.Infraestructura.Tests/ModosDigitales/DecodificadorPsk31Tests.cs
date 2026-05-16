using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Psk31;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador PSK31.
/// Genera audio sintetico con cambios de fase BPSK para verificar la decodificacion Varicode.
/// </summary>
public class DecodificadorPsk31Tests
{
    private const int FrecuenciaMuestreo = 12000;
    private const double FrecuenciaPortadora = 1000.0;
    private const double BaudRate = 31.25;

    private static readonly int MuestrasPorBit = (int)(FrecuenciaMuestreo / BaudRate);

    /// <summary>
    /// Genera muestras de portadora BPSK para un bit dado.
    /// Bit 1 = sin cambio de fase, Bit 0 = con cambio de fase (inversion).
    /// </summary>
    private static short[] GenerarBitBpsk(bool esBitUno, ref double fase)
    {
        if (!esBitUno)
        {
            fase += Math.PI; // Invertir fase para bit 0
        }

        short[] muestras = new short[MuestrasPorBit];
        for (int i = 0; i < MuestrasPorBit; i++)
        {
            double angulo = 2.0 * Math.PI * FrecuenciaPortadora * i / FrecuenciaMuestreo + fase;
            double valor = 16000.0 * Math.Cos(angulo);
            muestras[i] = (short)Math.Clamp(valor, short.MinValue, short.MaxValue);
        }

        return muestras;
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

    [Fact]
    public void Modo_DebeSerPSK()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();

        // Act & Assert
        decodificador.Modo.Should().Be(ModoOperacion.PSK);

        decodificador.Dispose();
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DebeSer12000()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();

        // Act & Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(12000);

        decodificador.Dispose();
    }

    [Fact]
    public void SubModosSoportados_DebeContenerPSK31()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();

        // Act & Assert
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.PSK31);

        decodificador.Dispose();
    }

    [Fact]
    public async Task IniciarAsync_CambiaEstadoAActivo()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();

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
        DecodificadorPsk31 decodificador = new();
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
        DecodificadorPsk31 decodificador = new();
        double fase = 0.0;
        short[] datos = GenerarBitBpsk(true, ref fase);
        MuestraAudio muestra = new(new ReadOnlyMemory<short>(datos), FrecuenciaMuestreo, DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        resultado.Should().BeEmpty();

        decodificador.Dispose();
    }

    [Fact]
    public async Task ProcesarAudio_SeñalBpsk_NoCrashea()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();
        await decodificador.IniciarAsync();

        // Generar una secuencia de bits BPSK con cambios de fase
        double fase = 0.0;
        List<short[]> segmentos = new();

        // Generar algunos bits: 1,1,0,1,1,0,0 (separador) seguido de mas bits
        bool[] bits = { true, true, false, true, true, false, false, true, false, true, true, false, false };
        foreach (bool bit in bits)
        {
            segmentos.Add(GenerarBitBpsk(bit, ref fase));
        }

        short[] audioCompleto = Concatenar(segmentos.ToArray());
        MuestraAudio muestra = new(
            new ReadOnlyMemory<short>(audioCompleto),
            FrecuenciaMuestreo,
            DateTimeOffset.UtcNow);

        // Act
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        // Assert — no debe lanzar excepcion
        decodificador.EstaActivo.Should().BeTrue();

        decodificador.Dispose();
    }

    [Fact]
    public void TablaVaricode_DecodificarEspacio_DevuelveEspacio()
    {
        // Arrange — espacio en Varicode es "1" (un solo bit 1)
        List<bool> bits = new() { true };

        // Act
        char? resultado = TablaVaricode.DecodificarVaricode(bits);

        // Assert
        resultado.Should().Be(' ');
    }

    [Fact]
    public void TablaVaricode_DecodificarE_DevuelveE()
    {
        // Arrange — 'e' en Varicode es "11" (dos bits 1)
        List<bool> bits = new() { true, true };

        // Act
        char? resultado = TablaVaricode.DecodificarVaricode(bits);

        // Assert
        resultado.Should().Be('e');
    }

    [Fact]
    public void TablaVaricode_DecodificarT_DevuelveT()
    {
        // Arrange — 't' en Varicode es "101"
        List<bool> bits = new() { true, false, true };

        // Act
        char? resultado = TablaVaricode.DecodificarVaricode(bits);

        // Assert
        resultado.Should().Be('t');
    }

    [Fact]
    public void TablaVaricode_DecodificarO_DevuelveO()
    {
        // Arrange — 'o' en Varicode es "111"
        List<bool> bits = new() { true, true, true };

        // Act
        char? resultado = TablaVaricode.DecodificarVaricode(bits);

        // Assert
        resultado.Should().Be('o');
    }

    [Fact]
    public void TablaVaricode_BitsVacios_DevuelveNull()
    {
        // Arrange
        List<bool> bits = new();

        // Act
        char? resultado = TablaVaricode.DecodificarVaricode(bits);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void TablaVaricode_BitsNull_DevuelveNull()
    {
        // Act
        char? resultado = TablaVaricode.DecodificarVaricode(null!);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void TablaVaricode_ObtenerCodigo_DevuelveCodigoCorrecto()
    {
        // Arrange & Act
        bool[]? codigoEspacio = TablaVaricode.ObtenerCodigo(' ');
        bool[]? codigoE = TablaVaricode.ObtenerCodigo('e');

        // Assert
        codigoEspacio.Should().NotBeNull();
        codigoEspacio.Should().HaveCount(1);
        codigoEspacio![0].Should().BeTrue();

        codigoE.Should().NotBeNull();
        codigoE.Should().HaveCount(2);
        codigoE![0].Should().BeTrue();
        codigoE[1].Should().BeTrue();
    }

    [Fact]
    public void TablaVaricode_ObtenerCodigo_CaracterFueraDeRango_DevuelveNull()
    {
        // Act
        bool[]? resultado = TablaVaricode.ObtenerCodigo((char)200);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ConfiguracionPsk31_ValoresPorDefecto_Correctos()
    {
        // Arrange & Act
        ConfiguracionPsk31 config = new();

        // Assert
        config.FrecuenciaPortadora.Should().Be(1000.0);
        config.TasaDeMuestreo.Should().Be(12_000);
        config.BaudRate.Should().Be(31.25);
        config.UmbralDeteccion.Should().Be(0.5);
    }

    [Fact]
    public void Dispose_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();

        // Act & Assert
        Action accion = () => decodificador.Dispose();
        accion.Should().NotThrow();
    }

    [Fact]
    public void Dispose_DobleDispose_NoLanzaExcepcion()
    {
        // Arrange
        DecodificadorPsk31 decodificador = new();

        // Act & Assert
        Action accion = () =>
        {
            decodificador.Dispose();
            decodificador.Dispose();
        };
        accion.Should().NotThrow();
    }
}
