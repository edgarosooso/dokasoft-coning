using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControladorModoSolo : MonoBehaviour
{
    [Header("UI y Referencias")]
    public Button botonModoSolo;         // Arrastra tu botón del Lobby aquí
    public GestorTablero gestorTablero;    // Arrastra el objeto que tiene el script GestorTablero
    public GameObject panelLobby;        // El panel actual del lobby para ocultarlo
    public GameObject panelJuego;        // El panel de la matriz 5x4 para mostrarlo

    void Start()
    {
        // Esperamos un momento o verificamos la conexión antes de suscribirnos
        StartCoroutine(ConectarEventosConRetraso());

        if (botonModoSolo != null)
        {
            botonModoSolo.onClick.AddListener(SolicitarModoIndividual);
        }
    }

    System.Collections.IEnumerator ConectarEventosConRetraso()
    {
        // Esperamos a que el ControladorJuego y el socket existan y estén conectados
        while (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null)
        {
            yield return null;
        }

        EscucharEventosSocket();
        Debug.Log("Eventos de socket suscritos correctamente en ControladorModoSolo.");
    }

    void SolicitarModoIndividual()
    {
        Debug.Log("Enviando petición 'iniciar_modo_solo' al servidor...");

        // Verificamos que el ControladorJuego y su socket existan y estén conectados
        if (ControladorJuego.Instance != null && ControladorJuego.Instance.socket != null)
        {
            // Emitimos el evento exactamente como lo recibe tu servidor Node.js
            ControladorJuego.Instance.socket.Emit("iniciar_modo_solo");
        }
        else
        {
            Debug.LogError("El socket no está inicializado en ControladorJuego.");
        }
    }

    void EscucharEventosSocket()
    {
        if (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null) return;

        // Escuchamos el evento de inicio de partida que viene del servidor
        ControladorJuego.Instance.socket.OnUnityThread("iniciar_partida", (response) =>
        {
            try
            {
                Debug.Log("--- EVENTO INICIAR_PARTIDA RECIBIDO ---");

                // 1. Ver el JSON crudo que manda Node.js
                string rawJson = response != null ? response.ToString() : "RESPONSE ES NULO";
                Debug.Log("JSON Crudo recibido: " + rawJson);

                DatosPartidaRespuesta respuesta = null;

                // Deserializamos manejando el formato de Socket.IO igual que en tu ControladorJuego
                try
                {
                    respuesta = Newtonsoft.Json.JsonConvert.DeserializeObject<DatosPartidaRespuesta>(rawJson);
                    Debug.Log("Deserialización directa exitosa.");
                }
                catch (System.Exception exDirect)
                {
                    Debug.LogWarning("Falló deserialización directa, intentando con JArray... Motivo: " + exDirect.Message);
                    var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
                    if (tokenArray.Count > 0)
                    {
                        respuesta = tokenArray[0].ToObject<DatosPartidaRespuesta>();
                        Debug.Log("Deserialización con JArray exitosa.");
                    }
                }

                if (respuesta != null)
                {
                    Debug.Log($"Datos parseados -> Nivel: {respuesta.nivel} | Total Parejas: {respuesta.total_parejas}");

                    // CORREGIDO: Ahora validamos 'fichas' en lugar de 'configuracion'
                    if (respuesta.fichas != null)
                    {
                        Debug.Log($"¡La lista 'fichas' NO es nula! Contiene {respuesta.fichas.Count} elementos.");
                    }
                    else
                    {
                        Debug.LogError("¡ATENCIÓN! La lista 'fichas' llegó como NULA dentro del JSON mapeado.");
                    }

                    // 1. Cambiamos de panel
                    if (panelLobby != null) panelLobby.SetActive(false);
                    if (panelJuego != null) panelJuego.SetActive(true);

                    // 2. Configuramos el tablero enviándole la lista de fichas
                    if (gestorTablero != null)
                    {
                        gestorTablero.ConfigurarTablero(respuesta.fichas);
                    }
                    else
                    {
                        Debug.LogError("El gestorTablero está nulo en ControladorModoSolo.");
                    }
                }
                else
                {
                    Debug.LogError("El objeto 'respuesta' quedó nulo después de intentar deserializar.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'iniciar_partida' en modo solo: " + e.ToString());
            }
        });

        //------
        ControladorJuego.Instance.socket.OnUnityThread("error_partida", (response) =>
        {
            Debug.LogError("Error recibido del servidor: " + response.ToString());
        });
    }
}

[System.Serializable]
public class DatosPartidaRespuesta
{
    public int nivel;
    public int total_parejas;
    public List<ItemNivelRespuesta> fichas;
}