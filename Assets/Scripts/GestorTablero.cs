using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GestorTablero : MonoBehaviour
{
    public static GestorTablero Instance;

    [Header("Referencias de Victoria / Siguiente Nivel")]
    public GameObject panelNivelCompletado; // Arrastra tu panel aquí desde el Inspector
    private int parejasEncontradas = 0;
    private int totalParejasDelNivel = 0;
    [Header("Referencias de Paneles para Navegación")]
    public GameObject panelJuego;       // El panel donde está el tablero actual
    public GameObject panelLobby;       // Tu panel de menú principal / lobby

    [Header("Referencias de UI")]
    public Transform contenedorMatriz;
    public GameObject prefabCuadro;

    // Control de selección de parejas
    private ControladorFicha primeraSeleccion = null;
    private ControladorFicha segundaSeleccion = null;
    private bool bloqueandoClics = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public void ConfigurarTablero(List<ItemNivelRespuesta> listaFichas)
    {
        if (listaFichas == null)
        {
            Debug.LogError("¡La lista de fichas llegó NULA!");
            return;
        }

        Debug.Log("Total de fichas a pintar: " + listaFichas.Count);

        if (contenedorMatriz == null)
        {
            Debug.LogError("¡Falta asignar el Contenedor Matriz en el Inspector de GestorTablero!");
            return;
        }

        if (prefabCuadro == null)
        {
            Debug.LogError("¡Falta asignar el Prefab Cuadro en el Inspector de GestorTablero!");
            return;
        }

        // Limpiamos fichas anteriores
        foreach (Transform child in contenedorMatriz)
        {
            Destroy(child.gameObject);
        }

        // Reiniciamos estados de selección
        primeraSeleccion = null;
        segundaSeleccion = null;
        bloqueandoClics = false;

        // Reiniciamos contadores de victoria para el nuevo nivel
        parejasEncontradas = 0;
        totalParejasDelNivel = listaFichas.Count / 2; // El total de parejas es la mitad de las fichas del nivel

        if (panelNivelCompletado != null)
            panelNivelCompletado.SetActive(false); // Aseguramos que el aviso inicie oculto

        // Instanciamos las nuevas fichas
        foreach (var ficha in listaFichas)
        {
            GameObject nuevoCuadro = Instantiate(prefabCuadro, contenedorMatriz);

            // Forzamos la escala normalizada
            nuevoCuadro.transform.localScale = Vector3.one;

            // Obtenemos el script de control de la ficha
            ControladorFicha controladorFicha = nuevoCuadro.GetComponent<ControladorFicha>();
            if (controladorFicha != null)
            {
                controladorFicha.ConfigurarFicha(ficha.id, ficha.texto, ficha.audio);
            }
        }
    }

     
    // Método que llama cada ControladorFicha al hacerle clic
    // public void FichaSeleccionada(ControladorFicha ficha)
    // {
    //     if (bloqueandoClics || ficha == primeraSeleccion) return;

    //     ficha.RevelarFicha();

    //     if (primeraSeleccion == null)
    //     {
    //         primeraSeleccion = ficha;
    //     }
    //     else
    //     {
    //         segundaSeleccion = ficha;
    //         bloqueandoClics = true;

    //         // Evaluamos si hacen pareja
    //         StartCoroutine(EvaluarParejaCo());
    //     }
    // }
    public void FichaSeleccionada(ControladorFicha ficha)
{
    // Si estamos evaluando una pareja, o la ficha ya está volteada/eliminada, ignoramos el clic
    if (bloqueandoClics || ficha == primeraSeleccion) return;

    ficha.RevelarFicha();

    if (primeraSeleccion == null)
    {
        primeraSeleccion = ficha;
    }
    else
    {
        segundaSeleccion = ficha;
        bloqueandoClics = true; // 👈 Bloquea inmediatamente el tablero

        StartCoroutine(EvaluarParejaCo());
    }
}
private System.Collections.IEnumerator EvaluarParejaCo()
{
    yield return new WaitForSeconds(0.8f); // Pausa breve para leer la palabra

    if (primeraSeleccion.idFicha == segundaSeleccion.idFicha)
    {
        Debug.Log("¡Pareja encontrada!");

        // Opción A: Desactivar los componentes visuales y de clic para que el espacio quede en blanco pero mantenga la estructura
        DesactivarFichaAcierto(primeraSeleccion);
        DesactivarFichaAcierto(segundaSeleccion);

        // 👇 AGREGA ESTAS LÍNEAS AQUÍ 👇
        parejasEncontradas++;

        if (parejasEncontradas >= totalParejasDelNivel)
        {
            Debug.Log("¡Nivel completado con éxito!");
            StartCoroutine(MostrarAvisoNivelCompletadoCo());
        }
        // 👆 ---------------------------- 👆
    }
    else
    {
        // Falló: se vuelven a ocultar
        primeraSeleccion.OcultarFicha();
        segundaSeleccion.OcultarFicha();
    }

    primeraSeleccion = null;
    segundaSeleccion = null;
    bloqueandoClics = false;
}

// Y asegúrate de tener este método auxiliar abajo en el mismo script:
private System.Collections.IEnumerator MostrarAvisoNivelCompletadoCo()
{
    yield return new WaitForSeconds(0.5f); // Pequeña pausa para que se alcance a ver la última ficha
    
    // 👇 AGREGA ESTA LÍNEA PARA OCULTAR EL TABLERO DE FICHAS 👇
    if (panelJuego != null)
    {
        panelJuego.SetActive(false);
    }

    // Muestra el panel de victoria
    if (panelNivelCompletado != null)
    {
        panelNivelCompletado.SetActive(true); 
    }
}

     
// Método auxiliar para apagar la ficha acertada sin romper el Grid Layout
    private void DesactivarFichaAcierto(ControladorFicha ficha)
    {
        if (ficha != null)
        {
            // Llama a la función de la ficha que apaga el FondoVisual, limpia el texto y desactiva el botón
            ficha.MarcarComoEncontrada();
        }
    }


    // 1. Recibe la ruta que viene de la base de datos (ej: "sonidos/niveles/nivel1/audio_01.mp3")
    public void ReproducirAudioDeFila(string rutaAudioRelativa)
    {
        rutaAudioRelativa = rutaAudioRelativa.TrimStart('/');
        string urlAudio = $"{AppConfig.BaseURL.Replace("/api", "")}/{rutaAudioRelativa}";

        StartCoroutine(DescargarYReproducirAudio(urlAudio));
    }

    // 2. Corrutina que descarga el audio del servidor y lo reproduce al instante
    private System.Collections.IEnumerator DescargarYReproducirAudio(string url)
    {
        Debug.Log("Descargando audio desde: " + url);

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al descargar el audio del servidor: " + www.error);
            }
            else
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);

                if (clip != null)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = gameObject.AddComponent<AudioSource>();
                    }

                    audioSource.PlayOneShot(clip);
                    Debug.Log("<color=green>¡Audio reproducido exitosamente desde el servidor!</color>");
                }
            }
        }
    }

    // Método que asignas al botón "Sí"
    // Método que asignas al botón "Sí" (BtnSiguiente)
    public void BotonSiguienteNivel_Click()
    {
        Debug.Log("Cargando el siguiente nivel...");

        // Ocultamos el panel de victoria actual
        if (panelNivelCompletado != null)
            panelNivelCompletado.SetActive(false);

        // Aquí llamas a la función que pide el Nivel 2 a tu servidor/API.
        // Por ejemplo, si tienes una variable de nivel actual, la incrementas y vuelves a pedir:
        // nivelActual++;
        // CargarDatosNivel(nivelActual);
    }

    // Método que asignas al botón "No" (BtnPrincipal)
    public void BotonMenuPrincipal_Click()
    {
        Debug.Log("Regresando al menú principal...");

        // Ocultamos el panel de victoria y el panel de juego actual
        if (panelNivelCompletado != null)
            panelNivelCompletado.SetActive(false);

        if (panelJuego != null)
            panelJuego.SetActive(false);

        // Activamos tu panel de lobby que se ve en la jerarquía
        if (panelLobby != null)
            panelLobby.SetActive(true);
    }
    }
