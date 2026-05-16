using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Nativo.ModosDigitales;

/// <summary>
/// Implementacion del registro central de decodificadores digitales.
/// Permite registrar, descubrir y obtener decodificadores por modo de operacion.
/// </summary>
public sealed class RegistroDecodificadores : IRegistroDecodificadores
{
    private readonly List<IDecodificadorDigital> _decodificadores = new();
    private readonly object _lock = new();

    /// <summary>
    /// Crea una nueva instancia del registro de decodificadores.
    /// </summary>
    public RegistroDecodificadores()
    {
    }

    /// <summary>
    /// Crea una nueva instancia del registro de decodificadores con una lista inicial.
    /// </summary>
    /// <param name="decodificadores">Decodificadores iniciales a registrar.</param>
    public RegistroDecodificadores(IEnumerable<IDecodificadorDigital> decodificadores)
    {
        if (decodificadores is null)
        {
            throw new ArgumentNullException(nameof(decodificadores));
        }

        foreach (IDecodificadorDigital decodificador in decodificadores)
        {
            _decodificadores.Add(decodificador);
        }
    }

    /// <summary>
    /// Registra un nuevo decodificador en el registro.
    /// Si ya existe un decodificador para el mismo modo, no se reemplaza.
    /// </summary>
    /// <param name="decodificador">Decodificador a registrar.</param>
    public void Registrar(IDecodificadorDigital decodificador)
    {
        if (decodificador is null)
        {
            throw new ArgumentNullException(nameof(decodificador));
        }

        lock (_lock)
        {
            _decodificadores.Add(decodificador);
        }
    }

    /// <summary>
    /// Obtiene todos los decodificadores registrados.
    /// </summary>
    /// <returns>Lista de solo lectura con todos los decodificadores.</returns>
    public IReadOnlyList<IDecodificadorDigital> ObtenerTodos()
    {
        lock (_lock)
        {
            return _decodificadores.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Obtiene un decodificador por modo de operacion.
    /// </summary>
    /// <param name="modo">Modo de operacion a buscar.</param>
    /// <returns>El decodificador para el modo especificado, o null si no existe.</returns>
    public IDecodificadorDigital? ObtenerPorModo(ModoOperacion modo)
    {
        lock (_lock)
        {
            foreach (IDecodificadorDigital decodificador in _decodificadores)
            {
                if (decodificador.Modo == modo)
                {
                    return decodificador;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Obtiene los modos de operacion actualmente disponibles.
    /// </summary>
    /// <returns>Lista de modos para los cuales hay decodificadores registrados.</returns>
    public IReadOnlyList<ModoOperacion> ObtenerModosDisponibles()
    {
        lock (_lock)
        {
            List<ModoOperacion> modos = new();

            foreach (IDecodificadorDigital decodificador in _decodificadores)
            {
                if (!modos.Contains(decodificador.Modo))
                {
                    modos.Add(decodificador.Modo);
                }
            }

            return modos.AsReadOnly();
        }
    }
}
