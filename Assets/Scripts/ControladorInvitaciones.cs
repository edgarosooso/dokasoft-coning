using UnityEngine;
using TMPro;

public class ControladorInvitaciones : MonoBehaviour
{
    [Header("UI del Panel")]
    public GameObject panelVentanaMensaje;       // Arrastra aquí tu objeto "VentanaMensaje"
    public TextMeshProUGUI textoMensaje;          // Arrastra aquí el componente TextMeshPro del mensaje

    private string idJugadorObjetivo;             // El ID del jugador al que vamos a invitar

    void Start()
    {
        // Asegurarnos de que el panel arranque apagado al iniciar la escena
        if (panelVentanaMensaje != null)
        {
            panelVentanaMensaje.SetActive(false);
        }
    }

    // Método para abrir la ventana y poner el nombre del jugador seleccionado
    public void AbrirVentanaInvitacion(string idJugador, string nombreJugador)
    {
        idJugadorObjetivo = idJugador;

        if (textoMensaje != null)
        {
            textoMensaje.text = $"¿Deseas enviar una invitación a jugar a {nombreJugador}?";
        }

        if (panelVentanaMensaje != null)
        {
            panelVentanaMensaje.SetActive(true); // Muestra el panel en pantalla
        }
    }

    // Método que se ejecuta cuando haces clic en el botón "Aceptar"
    // Método que se ejecuta cuando haces clic en el botón "Aceptar"
    // Método que se ejecuta cuando haces clic en el botón "Aceptar"
    public void EnviarInvitacionConfirmada()
    {
        if (ControladorJuego.Instance != null)
        {
            string idLimpio = !string.IsNullOrEmpty(idJugadorObjetivo) ? idJugadorObjetivo : "1";

            Debug.Log($"---> Enviando ID emisor limpio a Node.js: {idLimpio}");

            // Enviamos un objeto anónimo directo. Socket.io lo serializará perfectamente como { "idEmisor": "valor" }
            var datos = new { idEmisor = idLimpio };

            ControladorJuego.Instance.socket.Emit("aceptar_invitacion", datos);
        }

        CerrarVentana();
    }
    // Método para cerrar o cancelar la ventana
    public void CerrarVentana()
    {
        if (panelVentanaMensaje != null)
        {
            panelVentanaMensaje.SetActive(false);
        }
    }
}