using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Psk250;
using RadioAficionado.Nativo.ModosDigitales.Mfsk128;
using RadioAficionado.Nativo.ModosDigitales.Thor;
using RadioAficionado.Nativo.ModosDigitales.DominoEx;
using RadioAficionado.Nativo.ModosDigitales.Fsq;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para los decodificadores PSK250, MFSK128, THOR, DominoEX y FSQ.
/// Verifica propiedades basicas, ciclo de vida y procesamiento de audio sin crash.
/// </summary>
public class DecodificadoresAdicionalesTests
{
    private static MuestraAudio CrearMuestraSilencio(int muestras = 4800, int tasaMuestreo = 12000)
    {
        short[] datos = new short[muestras];
        return new MuestraAudio(new ReadOnlyMemory<short>(datos), tasaMuestreo, DateTimeOffset.UtcNow);
    }

    private static MuestraAudio CrearMuestraTono(double frecuenciaHz = 1000.0, int muestras = 4800, int tasaMuestreo = 12000)
    {
        short[] datos = new short[muestras];
        for (int i = 0; i < muestras; i++)
        {
            double angulo = 2.0 * Math.PI * frecuenciaHz * i / tasaMuestreo;
            datos[i] = (short)(8000.0 * Math.Sin(angulo));
        }

        return new MuestraAudio(new ReadOnlyMemory<short>(datos), tasaMuestreo, DateTimeOffset.UtcNow);
    }

    // ========== PSK250 ==========

    [Fact]
    public void Psk250_Modo_DebeSerPSK()
    {
        using DecodificadorPsk250 decodificador = new();
        decodificador.Modo.Should().Be(ModoOperacion.PSK);
    }

    [Fact]
    public void Psk250_SubModosSoportados_DebeContenerPSK250()
    {
        using DecodificadorPsk250 decodificador = new();
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.PSK250);
    }

    [Fact]
    public void Psk250_TasaDeMuestreo_DebeSer12000()
    {
        using DecodificadorPsk250 decodificador = new();
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(12000);
    }

    [Fact]
    public async Task Psk250_IniciarYDetener_CambiaEstado()
    {
        using DecodificadorPsk250 decodificador = new();

        await decodificador.IniciarAsync();
        decodificador.EstaActivo.Should().BeTrue();

        await decodificador.DetenerAsync();
        decodificador.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task Psk250_ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        using DecodificadorPsk250 decodificador = new();
        MuestraAudio muestra = CrearMuestraSilencio();

        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Psk250_ProcesarAudio_ConTono_NoCrashea()
    {
        using DecodificadorPsk250 decodificador = new();
        await decodificador.IniciarAsync();

        MuestraAudio muestra = CrearMuestraTono();
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        decodificador.EstaActivo.Should().BeTrue();
    }

    [Fact]
    public void Psk250_Configuracion_ValoresPorDefecto()
    {
        ConfiguracionPsk250 config = new();
        config.BaudRate.Should().Be(250.0);
        config.TasaDeMuestreo.Should().Be(12000);
    }

    // ========== MFSK128 ==========

    [Fact]
    public void Mfsk128_Modo_DebeSerMFSK()
    {
        using DecodificadorMfsk128 decodificador = new();
        decodificador.Modo.Should().Be(ModoOperacion.MFSK);
    }

    [Fact]
    public void Mfsk128_SubModosSoportados_DebeContenerMFSK128()
    {
        using DecodificadorMfsk128 decodificador = new();
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.MFSK128);
    }

    [Fact]
    public async Task Mfsk128_IniciarYDetener_CambiaEstado()
    {
        using DecodificadorMfsk128 decodificador = new();

        await decodificador.IniciarAsync();
        decodificador.EstaActivo.Should().BeTrue();

        await decodificador.DetenerAsync();
        decodificador.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task Mfsk128_ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        using DecodificadorMfsk128 decodificador = new();
        MuestraAudio muestra = CrearMuestraSilencio();

        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Mfsk128_ProcesarAudio_ConTono_NoCrashea()
    {
        using DecodificadorMfsk128 decodificador = new();
        await decodificador.IniciarAsync();

        MuestraAudio muestra = CrearMuestraTono(1200.0);
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        decodificador.EstaActivo.Should().BeTrue();
    }

    // ========== THOR ==========

    [Fact]
    public void Thor_Modo_DebeSerTHOR()
    {
        using DecodificadorThor decodificador = new();
        decodificador.Modo.Should().Be(ModoOperacion.THOR);
    }

    [Fact]
    public void Thor_SubModosSoportados_DebeContenerTresSubmodos()
    {
        using DecodificadorThor decodificador = new();
        decodificador.SubModosSoportados.Should().HaveCount(3);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.THOR4);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.THOR8);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.THOR16);
    }

    [Fact]
    public async Task Thor_IniciarYDetener_CambiaEstado()
    {
        using DecodificadorThor decodificador = new();

        await decodificador.IniciarAsync();
        decodificador.EstaActivo.Should().BeTrue();

        await decodificador.DetenerAsync();
        decodificador.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task Thor_ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        using DecodificadorThor decodificador = new();
        MuestraAudio muestra = CrearMuestraSilencio();

        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Thor_ProcesarAudio_ConTono_NoCrashea()
    {
        using DecodificadorThor decodificador = new();
        await decodificador.IniciarAsync();

        MuestraAudio muestra = CrearMuestraTono(1100.0);
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        decodificador.EstaActivo.Should().BeTrue();
    }

    // ========== DominoEX ==========

    [Fact]
    public void DominoEx_Modo_DebeSerDOMINOEX()
    {
        using DecodificadorDominoEx decodificador = new();
        decodificador.Modo.Should().Be(ModoOperacion.DOMINO);
    }

    [Fact]
    public void DominoEx_SubModosSoportados_DebeContenerTresSubmodos()
    {
        using DecodificadorDominoEx decodificador = new();
        decodificador.SubModosSoportados.Should().HaveCount(3);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.DOMINOEX4);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.DOMINOEX8);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.DOMINOEX16);
    }

    [Fact]
    public async Task DominoEx_IniciarYDetener_CambiaEstado()
    {
        using DecodificadorDominoEx decodificador = new();

        await decodificador.IniciarAsync();
        decodificador.EstaActivo.Should().BeTrue();

        await decodificador.DetenerAsync();
        decodificador.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task DominoEx_ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        using DecodificadorDominoEx decodificador = new();
        MuestraAudio muestra = CrearMuestraSilencio();

        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task DominoEx_ProcesarAudio_ConTono_NoCrashea()
    {
        using DecodificadorDominoEx decodificador = new();
        await decodificador.IniciarAsync();

        MuestraAudio muestra = CrearMuestraTono(1050.0);
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        decodificador.EstaActivo.Should().BeTrue();
    }

    // ========== FSQ ==========

    [Fact]
    public void Fsq_Modo_DebeSerFSQ()
    {
        using DecodificadorFsq decodificador = new();
        decodificador.Modo.Should().Be(ModoOperacion.FSQ);
    }

    [Fact]
    public void Fsq_SubModosSoportados_DebeContenerCuatroSubmodos()
    {
        using DecodificadorFsq decodificador = new();
        decodificador.SubModosSoportados.Should().HaveCount(4);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.FSQ2);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.FSQ3);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.FSQ4_5);
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.FSQ6);
    }

    [Fact]
    public async Task Fsq_IniciarYDetener_CambiaEstado()
    {
        using DecodificadorFsq decodificador = new();

        await decodificador.IniciarAsync();
        decodificador.EstaActivo.Should().BeTrue();

        await decodificador.DetenerAsync();
        decodificador.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task Fsq_ProcesarAudio_SinIniciar_DevuelveVacio()
    {
        using DecodificadorFsq decodificador = new();
        MuestraAudio muestra = CrearMuestraSilencio();

        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Fsq_ProcesarAudio_ConTono_NoCrashea()
    {
        using DecodificadorFsq decodificador = new();
        await decodificador.IniciarAsync();

        MuestraAudio muestra = CrearMuestraTono(1500.0);
        IReadOnlyList<MensajeDecodificado> resultado = await decodificador.ProcesarAudioAsync(muestra);

        decodificador.EstaActivo.Should().BeTrue();
    }

    // ========== Dispose doble ==========

    [Fact]
    public void TodosLosDecodificadores_DisposeDobleSinExcepcion()
    {
        DecodificadorPsk250 psk250 = new();
        DecodificadorMfsk128 mfsk128 = new();
        DecodificadorThor thor = new();
        DecodificadorDominoEx dominoEx = new();
        DecodificadorFsq fsq = new();

        Action accion = () =>
        {
            psk250.Dispose(); psk250.Dispose();
            mfsk128.Dispose(); mfsk128.Dispose();
            thor.Dispose(); thor.Dispose();
            dominoEx.Dispose(); dominoEx.Dispose();
            fsq.Dispose(); fsq.Dispose();
        };

        accion.Should().NotThrow();
    }

    // ========== Submodos nuevos ==========

    [Fact]
    public void SubModo_PSK250_ModoPrincipalEsPSK()
    {
        SubModoOperacion.PSK250.ObtenerModoPrincipal().Should().Be(ModoOperacion.PSK);
    }

    [Fact]
    public void SubModo_MFSK128_ModoPrincipalEsMFSK()
    {
        SubModoOperacion.MFSK128.ObtenerModoPrincipal().Should().Be(ModoOperacion.MFSK);
    }

    [Fact]
    public void SubModo_THOR16_ModoPrincipalEsTHOR()
    {
        SubModoOperacion.THOR16.ObtenerModoPrincipal().Should().Be(ModoOperacion.THOR);
    }

    [Fact]
    public void SubModo_DOMINOEX8_ModoPrincipalEsDOMINOEX()
    {
        SubModoOperacion.DOMINOEX8.ObtenerModoPrincipal().Should().Be(ModoOperacion.DOMINO);
    }

    [Fact]
    public void SubModo_FSQ4_5_ModoPrincipalEsFSQ()
    {
        SubModoOperacion.FSQ4_5.ObtenerModoPrincipal().Should().Be(ModoOperacion.FSQ);
    }
}
