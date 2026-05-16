namespace RadioAficionado.Servicio.Dtos;

/// <summary>
/// Configuracion de conexion al radio enviada desde el cliente.
/// </summary>
/// <param name="UsarCatSerial">True para CAT serial directo, false para rigctld TCP.</param>
/// <param name="Puerto">Puerto COM (ej: "COM3") para CAT serial.</param>
/// <param name="Baudios">Velocidad de baudios para CAT serial.</param>
/// <param name="Modelo">Modelo de radio (ej: "Automatico", "Yaesu", "Icom").</param>
/// <param name="BitsDeDatos">Bits de datos (7 u 8).</param>
/// <param name="BitsDeParada">Bits de parada (1, 1.5, 2).</param>
/// <param name="Paridad">Paridad (None, Even, Odd).</param>
/// <param name="RtsEnable">Habilitar RTS.</param>
/// <param name="DtrEnable">Habilitar DTR.</param>
/// <param name="MetodoPtt">Metodo PTT: "CAT", "DTR", "RTS", "VOX".</param>
/// <param name="IntervaloPollingMs">Intervalo de polling en milisegundos.</param>
/// <param name="HostRigctld">Host de rigctld para modo TCP.</param>
/// <param name="PuertoRigctld">Puerto de rigctld para modo TCP.</param>
/// <param name="DispositivoAudioEntrada">ID del dispositivo de audio de entrada.</param>
/// <param name="TasaDeMuestreoHz">Tasa de muestreo del audio en Hz.</param>
public sealed record ConfiguracionConexionDto(
    bool UsarCatSerial,
    string Puerto,
    int Baudios,
    string Modelo,
    int BitsDeDatos,
    int BitsDeParada,
    string Paridad,
    bool RtsEnable,
    bool DtrEnable,
    string MetodoPtt,
    int IntervaloPollingMs,
    string HostRigctld,
    int PuertoRigctld,
    string DispositivoAudioEntrada,
    int TasaDeMuestreoHz);
