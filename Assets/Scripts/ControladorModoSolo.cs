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
#if UNITY_WEBGL && !UNITY_EDITOR
        // En WebGL esperamos a que la instancia exista (la conexión la maneja el JSBridge)
        while (ControladorJuego.Instance == null)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
#else
        // En Android / PC / Editor esperamos al socket de C# como antes
        while (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null)
        {
            yield return null;
        }

        // Escuchamos errores particulares del modo solo si el servidor los emite (Solo APK/PC)
        ControladorJuego.Instance.socket.OnUnityThread("error_partida", (response) =>
        {
            Debug.LogError("Error recibido del servidor en modo solo: " + response.ToString());
        });
#endif
    }

    public void SolicitarModoMaquina()
    {
        SolicitarModoMaquina(1);
    }

    public void SolicitarModoMaquina(int dificultad)
    {
        int nivelSeleccionado = PlayerPrefs.GetInt(MenuNiveles.ClaveNivelSeleccionado,
            gestorTablero != null ? gestorTablero.nivelActual : 1);
        nivelSeleccionado = Mathf.Max(1, nivelSeleccionado);

        if (gestorTablero != null)
            gestorTablero.nivelActual = nivelSeleccionado;
        dificultad = Mathf.Clamp(dificultad, 1, 3);

        if (ControladorJuego.Instance != null)
        {
            ControladorJuego.Instance.esModoMultijugador = false;
            ControladorJuego.Instance.modoSoloSolicitado = true;
            ControladorJuego.Instance.modoMaquina = true;
            ControladorJuego.Instance.dificultadMaquina = dificultad;
        }

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.EstaConectado())
        {
            var datosPeticion = new
            {
                nivel = nivelSeleccionado,
                nivelActual = nivelSeleccionado,
                nivelSeleccionado = nivelSeleccionado,
                modo = "maquina",
                dificultad = dificultad
            };

            // Usamos el método unificado en lugar de .socket.Emit
            ControladorJuego.Instance.EnviarEventoSocket("iniciar_modo_solo", datosPeticion);
            Debug.Log("Enviando petición para jugar contra la máquina en el nivel " + nivelSeleccionado);
        }
        else
        {
            Debug.LogError("El socket no está inicializado o conectado en ControladorJuego.");
        }
    }

    public void SolicitarModoIndividual()
    {
        int nivelSeleccionado = PlayerPrefs.GetInt(MenuNiveles.ClaveNivelSeleccionado,
            gestorTablero != null ? gestorTablero.nivelActual : 1);
        nivelSeleccionado = Mathf.Max(1, nivelSeleccionado);
        if (gestorTablero != null)
            gestorTablero.nivelActual = nivelSeleccionado;

        // Indicamos que NO estamos en modo multijugador
        if (ControladorJuego.Instance != null)
        {
            ControladorJuego.Instance.esModoMultijugador = false;
            ControladorJuego.Instance.modoSoloSolicitado = true;
            ControladorJuego.Instance.modoMaquina = false;
        }

        Debug.Log("Enviando petición 'iniciar_modo_solo' para el nivel seleccionado: " + nivelSeleccionado);

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.EstaConectado())
        {
            var datosPeticion = new
            {
                nivel = nivelSeleccionado,
                nivelActual = nivelSeleccionado,
                nivelSeleccionado = nivelSeleccionado
            };

            // Usamos el método unificado en lugar de .socket.Emit
            ControladorJuego.Instance.EnviarEventoSocket("iniciar_modo_solo", datosPeticion);
        }
        else
        {
            Debug.LogError("El socket no está inicializado o conectado en ControladorJuego.");
        }
    }
}