using UnityEngine;
using TMPro;

public class ControladorInvitaciones : MonoBehaviour
{
    // Instancia estática para que otros scripts (como jugadorPrefab) puedan llamarlo fácilmente
    public static ControladorInvitaciones Instance;

    [Header("UI del Panel de Invitación")]
    public GameObject panelVentanaMensaje;       // Arrastra aquí "VentanaMensaje"
    public TextMeshProUGUI textoMensaje;          // Arrastra aquí "TextoMensaje"

    [Header("UI de Cambio de Panel al Jugar")]
    public GameObject panelLobby;                 
    public GameObject panelJuego;                 
    public GestorTablero gestorTablero;           

    private string idJugadorObjetivo;             

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (panelVentanaMensaje != null)
        {
            panelVentanaMensaje.SetActive(false);
        }

        StartCoroutine(ConectarEventosSocketConRetraso());
    }

    System.Collections.IEnumerator ConectarEventosSocketConRetraso()
    {
        while (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null)
        {
            yield return null;
        }

        EscucharEventosSocketPareja();
        Debug.Log("Eventos de socket en pareja suscritos correctamente.");
    }

    // Método que abre la ventana con el nombre y el ID correcto
    public void AbrirVentanaInvitacion(string idJugador, string nombreJugador)
    {
        idJugadorObjetivo = idJugador;
        Debug.Log($"Abriendo ventana para ID: {idJugadorObjetivo}, Nombre: {nombreJugador}");

        if (textoMensaje != null)
        {
            textoMensaje.text = $"¿Deseas enviar una invitación a jugar a {nombreJugador}?";
        }

        if (panelVentanaMensaje != null)
        {
            panelVentanaMensaje.SetActive(true);
        }
    }

    // Método que se ejecuta al hacer clic en el botón "Aceptar"
   // Método que se ejecuta al hacer clic en el botón "Aceptar"
    public void EnviarInvitacionConfirmada()
    {
        Debug.Log($"Enviando invitación al servidor para el jugador ID: {idJugadorObjetivo}");

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.socket != null)
        {
            var datos = new { 
                idJugadorEmisor = ControladorJuego.Instance.id_player,
                idJugadorReceptor = idJugadorObjetivo,
                nombreEmisor = ControladorJuego.Instance.nombre_jugador
            };
            
            ControladorJuego.Instance.socket.Emit("enviar_invitacion", datos);
        }
        else
        {
            Debug.LogError("No se pudo enviar: El socket de ControladorJuego es nulo.");
        }

        CerrarVentana();
    }
    // Método que se ejecuta al hacer clic en el botón "Volver" o al finalizar
    public void CerrarVentana()
    {
        if (panelVentanaMensaje != null)
        {
            panelVentanaMensaje.SetActive(false);
            Debug.Log("Ventana de mensaje cerrada.");
        }
    }

    void EscucharEventosSocketPareja()
    {
        if (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null) return;

        ControladorJuego.Instance.socket.OnUnityThread("iniciar_partida", (response) =>
        {
            try
            {
                Debug.Log("--- PARTIDA MULTIJUGADOR INICIADA ---");
                string rawJson = response != null ? response.ToString() : "";
                DatosPartidaRespuesta respuesta = null;

                try
                {
                    respuesta = Newtonsoft.Json.JsonConvert.DeserializeObject<DatosPartidaRespuesta>(rawJson);
                }
                catch
                {
                    var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
                    if (tokenArray.Count > 0)
                    {
                        respuesta = tokenArray[0].ToObject<DatosPartidaRespuesta>();
                    }
                }

                if (respuesta != null)
                {
                    if (panelLobby != null) panelLobby.SetActive(false);
                    if (panelJuego != null) panelJuego.SetActive(true);

                    if (gestorTablero != null)
                    {
                        gestorTablero.ConfigurarTablero(respuesta.fichas);

                        if (respuesta.fichas.Count > 0)
                        {
                            gestorTablero.CargarNuevaPalabra(respuesta.fichas[0].traduccion);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'iniciar_partida': " + e.ToString());
            }
        });
    }
}