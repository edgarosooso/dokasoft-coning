using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControladorModoSolo : MonoBehaviour
{
    [Header("UI y Referencias")]
    public Button botonModoSolo;         // Arrastra tu botón del Lobby aquí
    public GestorTablero gestorTablero;    // Arrastra el objeto que tiene el script GestorTablero
    public GameObject panelLobby;         // El panel actual del lobby para ocultarlo
    public GameObject panelJuego;         // El panel de la matriz para mostrarlo

    void Start()
    {
        if (botonModoSolo != null)
        {
            botonModoSolo.onClick.AddListener(SolicitarModoIndividual);
        }

        // Nos suscribimos opcionalmente a errores de partida si el socket ya está listo
        StartCoroutine(ConectarEventosConRetraso());
    }

    System.Collections.IEnumerator ConectarEventosConRetraso()
    {
        while (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null)
        {
            yield return null;
        }

        // Escuchamos errores particulares del modo solo si el servidor los emite
        ControladorJuego.Instance.socket.OnUnityThread("error_partida", (response) =>
        {
            Debug.LogError("Error recibido del servidor en modo solo: " + response.ToString());
        });
    }

    public void SolicitarModoIndividual()
    {
        // Indicamos que NO estamos en modo multijugador
        if (ControladorJuego.Instance != null)
        {
            ControladorJuego.Instance.esModoMultijugador = false;
        }

        Debug.Log("Enviando petición 'iniciar_modo_solo' para el nivel: " + gestorTablero.nivelActual);

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.socket != null)
        {
            var datosPeticion = new { nivel = gestorTablero.nivelActual };
            ControladorJuego.Instance.socket.Emit("iniciar_modo_solo", datosPeticion);
        }
        else
        {
            Debug.LogError("El socket no está inicializado en ControladorJuego.");
        }
    }
}