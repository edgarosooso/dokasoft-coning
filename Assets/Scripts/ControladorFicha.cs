using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorFicha : MonoBehaviour
{
    [Header("Referencias de Texto")]
    public TextMeshProUGUI textoEspanol;        // Arriba (Español)
    public TextMeshProUGUI textoIngles;         // Centro (Inglés)
    public TextMeshProUGUI textoPronunciacion;  // Abajo (Fonética)

    public int indiceEnTablero;
    private Button botonFicha;
    private TextMeshProUGUI textoFicha; // Mantuvimos este por compatibilidad interna si se requiere

    [Header("Referencias Visuales")]
    [Tooltip("Arrastra aquí el objeto hijo 'FondoVisual' desde el inspector de Unity")]
    public GameObject fondoVisual;

    public int idFicha;
    public string textoPalabra;
    public string traduccionFicha;
    public string pronunciacionFicha; // 👈 Variable agregada y asegurada
    public string rutaAudio;
    public bool estaVolteada = false;
    public bool estaEliminada = false;

    void Awake()
    {
        botonFicha = GetComponent<Button>();

        if (fondoVisual == null && transform.childCount > 0)
        {
            Transform hijoFondo = transform.Find("FondoVisual");
            if (hijoFondo != null) fondoVisual = hijoFondo.gameObject;
        }

        textoFicha = GetComponentInChildren<TextMeshProUGUI>();

        if (botonFicha != null)
        {
            botonFicha.onClick.RemoveAllListeners();
            botonFicha.onClick.AddListener(AlHacerClic);
        }
    }

    public void ConfigurarFicha(int id, string texto, string audio, string traduccion, string pronunciacion, int indicePos)
    {
        idFicha = id;
        textoPalabra = texto;                 // Inglés (Centro)
        rutaAudio = audio;
        traduccionFicha = traduccion;         // Español (Arriba)
        pronunciacionFicha = pronunciacion;   // Fonética (Abajo)
        indiceEnTablero = indicePos;

        estaEliminada = false;

        if (fondoVisual != null) fondoVisual.SetActive(true);

        // 🔍 DEBUG PARA VERIFICAR QUE LLEGAN LOS DATOS
        Debug.Log($"🧩 [Ficha ID {id}] -> Español: '{traduccionFicha}' | Inglés: '{textoPalabra}' | Pronunciación: '{pronunciacionFicha}'");

        // Asignación a los componentes visuales de la tarjeta
        if (textoEspanol != null) textoEspanol.text = traduccionFicha;
        if (textoIngles != null) textoIngles.text = textoPalabra;
        if (textoPronunciacion != null) textoPronunciacion.text = pronunciacionFicha;

        OcultarFicha();
    }

    public void AlHacerClic()
    {
        if (estaEliminada || estaVolteada) return;

        if (GestorTablero.Instance != null && !GestorTablero.Instance.PuedeRecibirClic())
            return;

        if (ControladorJuego.Instance != null && !ControladorJuego.Instance.esModoMultijugador &&
            GestorTablero.Instance != null && !GestorTablero.Instance.PuedeRecibirClicLocal())
            return;

        // 🛑 VALIDACIÓN DE TURNO EN MULTIJUGADOR
        if (ControladorJuego.Instance != null && ControladorJuego.Instance.esModoMultijugador)
        {
            if (ControladorJuego.Instance.turnoActual != ControladorJuego.Instance.id_player.ToString())
            {
                Debug.Log("⏳ No es tu turno, espera a que juegue el rival.");
                return;
            }
        }

        Debug.Log("¡CLIC EXITOSO EN LA FICHA: " + textoPalabra + " (Índice: " + indiceEnTablero + ")!");

        RevelarFicha();

        Debug.Log("¡RUTA DE ARCHIVO " + rutaAudio);

        if (!string.IsNullOrEmpty(rutaAudio))
        {
            GestorTablero.Instance.ReproducirAudioDeFila(rutaAudio);
        }

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.esModoMultijugador)
        {
            var datosClic = new
            {
                nombreSala = ControladorJuego.Instance.nombreSalaActual,
                indiceFicha = indiceEnTablero,
                idJugador = ControladorJuego.Instance.id_player
            };

            ControladorJuego.Instance.EnviarEventoSocket("procesar_clic_ficha", datosClic);
        }

        if (GestorTablero.Instance != null)
        {
            GestorTablero.Instance.FichaSeleccionada(this);
        }
    }

   public void RevelarFicha()
{
    estaVolteada = true;

    if (textoEspanol != null) textoEspanol.text = traduccionFicha;
    if (textoIngles != null) textoIngles.text = textoPalabra;
    if (textoPronunciacion != null) textoPronunciacion.text = pronunciacionFicha;

    if (fondoVisual != null)
    {
        fondoVisual.SetActive(false);
    }

    // 🔍 DEPURACIÓN PARA VER EL ERROR EN LA CONSOLA
   /*
    if (GestorPopUpEstudio.Instance != null)
    {
        Debug.Log("✅ ¡GestorPopUpEstudio encontrado! Abriendo pop-up con: " + traduccionFicha);
        GestorPopUpEstudio.Instance.MostrarPopUp(traduccionFicha, textoPalabra, pronunciacionFicha);
    }
    else
    {
        Debug.LogError("❌ ERROR CRÍTICO: GestorPopUpEstudio.Instance es NULL. El script no está en la escena o no se inicializó en el Awake.");
    }
*/
}

    public void RevelarFichaRemota()
    {
        if (estaEliminada || estaVolteada) return;

        RevelarFicha();

        if (!string.IsNullOrEmpty(rutaAudio) && GestorTablero.Instance != null)
        {
            GestorTablero.Instance.ReproducirAudioDeFila(rutaAudio);
        }
    }

    public void OcultarFicha()
    {
        if (estaEliminada) return;

        estaVolteada = false;

        // Limpiamos los tres campos de texto al ocultar la tarjeta
        if (textoEspanol != null) textoEspanol.text = "";
        if (textoIngles != null) textoIngles.text = "";
        if (textoPronunciacion != null) textoPronunciacion.text = "";

        if (fondoVisual != null)
        {
            fondoVisual.SetActive(true);
        }
    }

    public void MarcarComoEncontrada()
    {
        estaEliminada = true;

        if (botonFicha != null)
        {
            botonFicha.interactable = false;
            var imgBot = botonFicha.GetComponent<Image>();
            if (imgBot != null) imgBot.enabled = false;
        }

        if (textoEspanol != null) textoEspanol.text = "";
        if (textoIngles != null) textoIngles.text = "";
        if (textoPronunciacion != null) textoPronunciacion.text = "";

        if (fondoVisual != null)
            fondoVisual.SetActive(false);
    }
}