using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.Rig;

namespace RadioAficionado.Dominio.Tests.Rig;

/// <summary>
/// Tests unitarios para el mapeo de modos entre rigctld (Hamlib) y el dominio.
/// Cubre conversiones DesdeRigctld, HaciaRigctld, VFO y conversion de dBm a unidades S.
/// </summary>
public class MapeadorModosTests
{
    [Theory]
    [InlineData("USB", ModoOperacion.SSB)]
    [InlineData("LSB", ModoOperacion.SSB)]
    [InlineData("CW", ModoOperacion.CW)]
    [InlineData("CWR", ModoOperacion.CW)]
    [InlineData("AM", ModoOperacion.AM)]
    [InlineData("FM", ModoOperacion.FM)]
    [InlineData("WFM", ModoOperacion.FM)]
    [InlineData("RTTY", ModoOperacion.RTTY)]
    [InlineData("RTTYR", ModoOperacion.RTTY)]
    [InlineData("PKTUSB", ModoOperacion.PKT)]
    [InlineData("PKTLSB", ModoOperacion.PKT)]
    [InlineData("PKTFM", ModoOperacion.PKT)]
    [InlineData("FT8", ModoOperacion.FT8)]
    [InlineData("FT4", ModoOperacion.FT4)]
    [InlineData("PSK", ModoOperacion.PSK)]
    [InlineData("PSK31", ModoOperacion.PSK)]
    [InlineData("PSK63", ModoOperacion.PSK)]
    [InlineData("PSK125", ModoOperacion.PSK)]
    [InlineData("MFSK", ModoOperacion.MFSK)]
    [InlineData("OLIVIA", ModoOperacion.OLIVIA)]
    [InlineData("JT65", ModoOperacion.JT65)]
    [InlineData("JT9", ModoOperacion.JT9)]
    [InlineData("WSPR", ModoOperacion.WSPR)]
    [InlineData("JS8", ModoOperacion.JS8)]
    [InlineData("MSK144", ModoOperacion.MSK144)]
    [InlineData("Q65", ModoOperacion.Q65)]
    [InlineData("FST4", ModoOperacion.FST4)]
    [InlineData("FST4W", ModoOperacion.FST4W)]
    [InlineData("SSTV", ModoOperacion.SSTV)]
    public void DesdeRigctld_ModoConocido_DevuelveModoCorrectDelDominio(string modoRigctld, ModoOperacion modoEsperado)
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld(modoRigctld);

        // Assert
        resultado.Modo.Should().Be(modoEsperado);
    }

    [Fact]
    public void DesdeRigctld_USB_SubModoEsUSB()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("USB");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.USB);
    }

    [Fact]
    public void DesdeRigctld_LSB_SubModoEsLSB()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("LSB");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.LSB);
    }

    [Fact]
    public void DesdeRigctld_PKTUSB_SubModoEsUSB()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("PKTUSB");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.USB);
    }

    [Fact]
    public void DesdeRigctld_PKTLSB_SubModoEsLSB()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("PKTLSB");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.LSB);
    }

    [Fact]
    public void DesdeRigctld_CW_SubModoEsNull()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("CW");

        // Assert
        resultado.SubModo.Should().BeNull();
    }

    [Fact]
    public void DesdeRigctld_PSK31_SubModoEsPSK31()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("PSK31");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.PSK31);
    }

    [Fact]
    public void DesdeRigctld_PSK63_SubModoEsPSK63()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("PSK63");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.PSK63);
    }

    [Fact]
    public void DesdeRigctld_PSK125_SubModoEsPSK125()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("PSK125");

        // Assert
        resultado.SubModo.Should().Be(SubModoOperacion.PSK125);
    }

    [Fact]
    public void DesdeRigctld_ModoDesconocido_DevuelveSsbUsbPorDefecto()
    {
        // Act
        MapeadorModos.ResultadoModo resultado = MapeadorModos.DesdeRigctld("MODOQUENOEXISTE");

        // Assert
        resultado.Modo.Should().Be(ModoOperacion.SSB);
        resultado.SubModo.Should().Be(SubModoOperacion.USB);
    }

    [Fact]
    public void DesdeRigctld_CadenaVacia_LanzaArgumentException()
    {
        // Act
        Action accion = () => MapeadorModos.DesdeRigctld("");

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DesdeRigctld_Null_LanzaArgumentException()
    {
        // Act
        Action accion = () => MapeadorModos.DesdeRigctld(null!);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HaciaRigctld_SsbUsb_DevuelveUSB()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.SSB, SubModoOperacion.USB);

        // Assert
        resultado.Should().Be("USB");
    }

    [Fact]
    public void HaciaRigctld_SsbLsb_DevuelveLSB()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.SSB, SubModoOperacion.LSB);

        // Assert
        resultado.Should().Be("LSB");
    }

    [Fact]
    public void HaciaRigctld_SsbSinSubModo_DevuelveUSB()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.SSB, null);

        // Assert
        resultado.Should().Be("USB");
    }

    [Fact]
    public void HaciaRigctld_Cw_DevuelveCW()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.CW, null);

        // Assert
        resultado.Should().Be("CW");
    }

    [Fact]
    public void HaciaRigctld_Am_DevuelveAM()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.AM, null);

        // Assert
        resultado.Should().Be("AM");
    }

    [Fact]
    public void HaciaRigctld_Fm_DevuelveFM()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.FM, null);

        // Assert
        resultado.Should().Be("FM");
    }

    [Fact]
    public void HaciaRigctld_Rtty_DevuelveRTTY()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.RTTY, null);

        // Assert
        resultado.Should().Be("RTTY");
    }

    [Fact]
    public void HaciaRigctld_FT8_DevuelvePKTUSB()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.FT8, null);

        // Assert
        resultado.Should().Be("PKTUSB");
    }

    [Fact]
    public void HaciaRigctld_PktLsb_DevuelvePKTLSB()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.PKT, SubModoOperacion.LSB);

        // Assert
        resultado.Should().Be("PKTLSB");
    }

    [Fact]
    public void HaciaRigctld_PskPsk63_DevuelvePSK63()
    {
        // Act
        string resultado = MapeadorModos.HaciaRigctld(ModoOperacion.PSK, SubModoOperacion.PSK63);

        // Assert
        resultado.Should().Be("PSK63");
    }

    [Theory]
    [InlineData(-73.0, 9)]
    [InlineData(-79.0, 8)]
    [InlineData(-85.0, 7)]
    [InlineData(-91.0, 6)]
    [InlineData(-97.0, 5)]
    [InlineData(-103.0, 4)]
    [InlineData(-109.0, 3)]
    [InlineData(-115.0, 2)]
    [InlineData(-121.0, 1)]
    [InlineData(-127.0, 0)]
    public void ConvertirDbmAUnidadesS_ValoresExactosDeTabla_DevuelveNivelSCorrecto(double dbm, int sEsperado)
    {
        // Act
        int resultado = MapeadorModos.ConvertirDbmAUnidadesS(dbm);

        // Assert
        resultado.Should().Be(sEsperado);
    }

    [Fact]
    public void ConvertirDbmAUnidadesS_ValorMuyBajo_DevuelveCero()
    {
        // Act
        int resultado = MapeadorModos.ConvertirDbmAUnidadesS(-200.0);

        // Assert
        resultado.Should().Be(0);
    }

    [Fact]
    public void ConvertirDbmAUnidadesS_ValorPorEncimaDeS9_DevuelveValorMayorQue9()
    {
        // Act: -63 dBm es S9+10dB
        int resultado = MapeadorModos.ConvertirDbmAUnidadesS(-63.0);

        // Assert
        resultado.Should().BeGreaterThan(9);
    }

    [Fact]
    public void VfoDesdeRigctld_VFOA_DevuelveA()
    {
        // Act
        char resultado = MapeadorModos.VfoDesdeRigctld("VFOA");

        // Assert
        resultado.Should().Be('A');
    }

    [Fact]
    public void VfoDesdeRigctld_VFOB_DevuelveB()
    {
        // Act
        char resultado = MapeadorModos.VfoDesdeRigctld("VFOB");

        // Assert
        resultado.Should().Be('B');
    }

    [Fact]
    public void VfoDesdeRigctld_Sub_DevuelveB()
    {
        // Act
        char resultado = MapeadorModos.VfoDesdeRigctld("Sub");

        // Assert
        resultado.Should().Be('B');
    }

    [Fact]
    public void VfoDesdeRigctld_Main_DevuelveA()
    {
        // Act
        char resultado = MapeadorModos.VfoDesdeRigctld("Main");

        // Assert
        resultado.Should().Be('A');
    }

    [Fact]
    public void VfoDesdeRigctld_CadenaVacia_DevuelveAPorDefecto()
    {
        // Act
        char resultado = MapeadorModos.VfoDesdeRigctld("");

        // Assert
        resultado.Should().Be('A');
    }

    [Fact]
    public void VfoDesdeRigctld_Null_DevuelveAPorDefecto()
    {
        // Act
        char resultado = MapeadorModos.VfoDesdeRigctld(null!);

        // Assert
        resultado.Should().Be('A');
    }

    [Fact]
    public void VfoHaciaRigctld_A_DevuelveVFOA()
    {
        // Act
        string resultado = MapeadorModos.VfoHaciaRigctld('A');

        // Assert
        resultado.Should().Be("VFOA");
    }

    [Fact]
    public void VfoHaciaRigctld_B_DevuelveVFOB()
    {
        // Act
        string resultado = MapeadorModos.VfoHaciaRigctld('B');

        // Assert
        resultado.Should().Be("VFOB");
    }

    [Fact]
    public void VfoHaciaRigctld_bMinuscula_DevuelveVFOB()
    {
        // Act
        string resultado = MapeadorModos.VfoHaciaRigctld('b');

        // Assert
        resultado.Should().Be("VFOB");
    }

    [Fact]
    public void VfoHaciaRigctld_CaracterDesconocido_DevuelveVFOAPorDefecto()
    {
        // Act
        string resultado = MapeadorModos.VfoHaciaRigctld('X');

        // Assert
        resultado.Should().Be("VFOA");
    }
}
