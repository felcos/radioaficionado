using Microsoft.Web.WebView2.WinForms;

namespace RadioAficionado.Lanzador;

/// <summary>
/// Ventana principal con WebView2 que muestra la UI web de RadioAficionado.
/// Modo app: sin barra de navegación, pantalla completa con F11.
/// </summary>
public sealed class VentanaPrincipal : Form
{
    private readonly WebView2 _webView;
    private readonly string _urlInicial;
    private readonly ConfiguracionLanzador _config;
    private bool _pantallaCompleta;
    private FormWindowState _estadoAnterior;
    private FormBorderStyle _bordesAnteriores;

    /// <summary>
    /// Crea la ventana principal con WebView2.
    /// </summary>
    /// <param name="urlInicial">URL a cargar al iniciar.</param>
    /// <param name="config">Configuracion persistida del lanzador.</param>
    public VentanaPrincipal(string urlInicial, ConfiguracionLanzador config)
    {
        _urlInicial = urlInicial;
        _config = config ?? throw new ArgumentNullException(nameof(config));

        Text = "RadioAficionado";
        Width = _config.Ancho;
        Height = _config.Alto;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = System.Drawing.Color.FromArgb(26, 26, 26);
        KeyPreview = true;

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(_webView);

        Load += VentanaPrincipal_Load;
        KeyDown += VentanaPrincipal_KeyDown;
    }

    // async void es el patron idiomatico para handlers de eventos de WinForms.
    // Toda la inicializacion async va envuelta en try/catch para que un fallo de
    // WebView2 no se convierta en una excepcion no observada que tumbe el proceso.
    private async void VentanaPrincipal_Load(object? sender, EventArgs e)
    {
        // Aplicar posicion guardada si es valida
        if (_config.PosicionX >= 0 && _config.PosicionY >= 0)
        {
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(_config.PosicionX, _config.PosicionY);
        }

        if (_config.Maximizada)
        {
            WindowState = FormWindowState.Maximized;
        }

        try
        {
            await _webView.EnsureCoreWebView2Async(null);

            // Configurar WebView2 como app (sin barra de navegación)
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = _config.DevToolsHabilitados;

            // Navegar a la URL del servicio
            _webView.CoreWebView2.Navigate(_urlInicial);

            // Actualizar título con el de la página
            _webView.CoreWebView2.DocumentTitleChanged += (s, args) =>
            {
                Text = _webView.CoreWebView2.DocumentTitle;
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo inicializar el navegador embebido (WebView2).\n\n{ex.Message}",
                "RadioAficionado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Close();
        }
    }

    private void VentanaPrincipal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            AlternarPantallaCompleta();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Alterna entre modo ventana y pantalla completa.
    /// </summary>
    private void AlternarPantallaCompleta()
    {
        if (_pantallaCompleta)
        {
            // Salir de pantalla completa
            FormBorderStyle = _bordesAnteriores;
            WindowState = _estadoAnterior;
            _pantallaCompleta = false;
        }
        else
        {
            // Entrar en pantalla completa
            _estadoAnterior = WindowState;
            _bordesAnteriores = FormBorderStyle;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            _pantallaCompleta = true;
        }
    }

    /// <summary>
    /// Guarda la configuracion de ventana (posicion, tamano, estado) antes de cerrar.
    /// </summary>
    /// <param name="e">Argumentos del evento de cierre.</param>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _config.Maximizada = WindowState == FormWindowState.Maximized;

        if (WindowState == FormWindowState.Normal)
        {
            _config.Ancho = Width;
            _config.Alto = Height;
            _config.PosicionX = Location.X;
            _config.PosicionY = Location.Y;
        }

        _config.Guardar();
        base.OnFormClosing(e);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _webView.Dispose();
        }

        base.Dispose(disposing);
    }
}
