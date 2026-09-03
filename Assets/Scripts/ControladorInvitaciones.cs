using UnityEngine;
using TMPro;

public class ControladorInvitaciones : MonoBehaviour
{
    // Instancia estática para que otros scripts (como jugadorPrefab) puedan llamarlo fácilmente
    public static ControladorInvitaciones Instance;

    [Header("UI del Panel de Invitación")]
    public GameObject panelVentanaMensaje;          // Arrastra aquí "VentanaMensaje"
    public TextMeshProUGUI textoMensaje;             // Arrastra aquí "TextoMensaje"

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
    public void EnviarInvitacionConfirmada()
    {
        Debug.Log($"Enviando invitación al servidor para el jugador ID: {idJugadorObjetivo}");

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.socket != null)
        {
            var datos = new
            {
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
}