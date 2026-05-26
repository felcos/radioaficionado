using RadioAficionado.Dominio.Alertas;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Servicio que evalua spots DX contra reglas de alerta configuradas por el operador.
/// Permite gestionar reglas y recibir notificaciones cuando un spot cumple una condicion.
/// </summary>
/// <remarks>
/// <para><b>Para que sirve:</b> Notificar al operador cuando un spot DX cumple condiciones configuradas
/// (entidad DXCC nueva, banda/modo especifico, indicativo concreto).</para>
/// <para><b>Como se usa:</b> Se inyecta por constructor. Se configuran reglas con los metodos de gestion
/// y se evaluan spots con <see cref="EvaluarSpot"/>. Se suscribe a <see cref="AlertaDisparada"/> para recibir notificaciones.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Alertas.ServicioAlertas</c>.</para>
/// <para><b>Registro DI:</b> Singleton en ConfiguracionServicios.</para>
/// <para><b>Dependencias:</b> <see cref="IRepositorioQso"/> para obtener QSOs y calcular DXCC faltantes.</para>
/// </remarks>
public interface IServicioAlertas
{
    /// <summary>
    /// Evalua un spot DX contra todas las reglas activas y dispara alertas si corresponde.
    /// </summary>
    /// <param name="spotteador">Indicativo del spotter.</param>
    /// <param name="dx">Indicativo de la estacion DX.</param>
    /// <param name="frecuenciaHz">Frecuencia en Hz.</param>
    /// <param name="comentario">Comentario del spot.</param>
    /// <param name="hora">Hora UTC del spot.</param>
    /// <returns>Lista de alertas disparadas (puede estar vacia).</returns>
    IReadOnlyList<ResultadoAlerta> EvaluarSpot(string spotteador, string dx, long frecuenciaHz, string comentario, DateTime hora);

    /// <summary>
    /// Agrega una nueva regla de alerta.
    /// </summary>
    /// <param name="regla">Regla a agregar.</param>
    void AgregarRegla(ReglaAlerta regla);

    /// <summary>
    /// Elimina una regla de alerta por su identificador.
    /// </summary>
    /// <param name="idRegla">Identificador de la regla a eliminar.</param>
    /// <returns>True si se elimino; false si no existia.</returns>
    bool EliminarRegla(Guid idRegla);

    /// <summary>
    /// Activa o desactiva una regla de alerta.
    /// </summary>
    /// <param name="idRegla">Identificador de la regla.</param>
    /// <param name="activa">True para activar; false para desactivar.</param>
    /// <returns>True si se encontro la regla; false si no existia.</returns>
    bool CambiarEstadoRegla(Guid idRegla, bool activa);

    /// <summary>
    /// Obtiene todas las reglas de alerta configuradas.
    /// </summary>
    /// <returns>Lista de reglas.</returns>
    IReadOnlyList<ReglaAlerta> ObtenerReglas();

    /// <summary>
    /// Evento que se dispara cuando un spot cumple una regla de alerta.
    /// </summary>
    event EventHandler<ResultadoAlerta>? AlertaDisparada;

    /// <summary>
    /// Actualiza la cache interna de entidades DXCC trabajadas.
    /// Debe llamarse cuando se registra un nuevo QSO o se importan contactos.
    /// </summary>
    /// <param name="numerosEntidadesTrabajadas">Numeros DXCC de las entidades ya trabajadas.</param>
    void ActualizarEntidadesTrabajadas(HashSet<int> numerosEntidadesTrabajadas);
}
