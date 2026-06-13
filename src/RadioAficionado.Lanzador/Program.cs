using System.Diagnostics;
using System.Net.Http;

namespace RadioAficionado.Lanzador;

/// <summary>
/// Punto de entrada del lanzador WebView2.
/// Arranca RadioAficionado.Servicio como proceso hijo y abre WebView2.
/// </summary>
internal static class Program
{
    /// <summary>Endpoint de health check del servicio.</summary>
    private const string RutaHealth = "/health";

    /// <summary>Timeout máximo para esperar al servicio en segundos.</summary>
    private const int TimeoutInicioSegundos = 30;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        ConfiguracionLanzador config = ConfiguracionLanzador.Cargar();
        string urlServicio = $"http://localhost:{config.Puerto}";

        Process? procesoServicio = null;

        try
        {
            procesoServicio = IniciarServicio();

            if (!EsperarServicioListo(urlServicio + RutaHealth))
            {
                MessageBox.Show(
                    $"No se pudo iniciar RadioAficionado.Servicio.\n" +
                    $"Verifique que el ejecutable exista y el puerto {config.Puerto} esté disponible.",
                    "RadioAficionado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            VentanaPrincipal ventana = new(urlServicio + "/Operacion", config);
            Application.Run(ventana);
        }
        finally
        {
            DetenerServicio(procesoServicio);
        }
    }

    /// <summary>
    /// Inicia RadioAficionado.Servicio como proceso hijo.
    /// </summary>
    private static Process? IniciarServicio()
    {
        string rutaServicio = ObtenerRutaServicio();

        if (!File.Exists(rutaServicio))
        {
            return null;
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = rutaServicio,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        return Process.Start(startInfo);
    }

    /// <summary>
    /// Espera a que el servicio responda al health check.
    /// Sincrono a proposito: se ejecuta antes de Application.Run, en el hilo STA,
    /// usando la API sincrona HttpClient.Send para no bloquear con sync-over-async.
    /// </summary>
    /// <param name="urlHealth">URL completa del endpoint de health check.</param>
    private static bool EsperarServicioListo(string urlHealth)
    {
        using HttpClient cliente = new();
        cliente.Timeout = TimeSpan.FromSeconds(5);

        DateTime limite = DateTime.UtcNow.AddSeconds(TimeoutInicioSegundos);

        while (DateTime.UtcNow < limite)
        {
            try
            {
                using HttpRequestMessage solicitud = new(HttpMethod.Get, urlHealth);
                using HttpResponseMessage respuesta = cliente.Send(solicitud);

                if (respuesta.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // El servicio aún no está listo.
            }
            catch (TaskCanceledException)
            {
                // Timeout en la petición, reintentar.
            }

            Thread.Sleep(500);
        }

        return false;
    }

    /// <summary>
    /// Detiene el proceso del servicio al cerrar la aplicación.
    /// </summary>
    private static void DetenerServicio(Process? proceso)
    {
        if (proceso is null || proceso.HasExited)
        {
            return;
        }

        try
        {
            proceso.Kill(entireProcessTree: true);
            proceso.WaitForExit(5000);
        }
        catch (InvalidOperationException)
        {
            // El proceso ya terminó.
        }
        finally
        {
            proceso.Dispose();
        }
    }

    /// <summary>
    /// Obtiene la ruta al ejecutable del servicio, relativa al lanzador.
    /// </summary>
    private static string ObtenerRutaServicio()
    {
        string directorio = AppContext.BaseDirectory;

        // Buscar en el directorio del lanzador o en un subdirectorio
        string rutaDirecta = Path.Combine(directorio, "RadioAficionado.Servicio.exe");
        if (File.Exists(rutaDirecta))
        {
            return rutaDirecta;
        }

        // Buscar en directorio hermano (despliegue separado)
        string? padre = Directory.GetParent(directorio)?.FullName;
        if (padre is not null)
        {
            string rutaHermano = Path.Combine(padre, "RadioAficionado.Servicio", "RadioAficionado.Servicio.exe");
            if (File.Exists(rutaHermano))
            {
                return rutaHermano;
            }
        }

        return rutaDirecta;
    }
}
