using UIKit;

namespace RadioAficionado.Mobile.iOS;

/// <summary>
/// Punto de entrada de la aplicación iOS.
/// </summary>
public static class Program
{
    /// <summary>
    /// Método principal que inicia la aplicación iOS con el delegado de Avalonia.
    /// </summary>
    /// <param name="args">Argumentos de línea de comandos.</param>
    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
