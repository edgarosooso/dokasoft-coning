using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorFicha : MonoBehaviour
{
    public int indiceEnTablero;
    private Button botonFicha;
    private TextMeshProUGUI textoFicha;
    public string traduccionFicha;

    [Header("Referencias Visuales")]
    [Tooltip("Arrastra aquí el objeto hijo 'FondoVisual' desde el inspector de Unity")]
    public GameObject fondoVisual;

    public int idFicha;
    public string textoPalabra;
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

    public void ConfigurarFicha(int id, string texto, string audio, string traduccion, int indicePos)
    {
        idFicha = id;
        textoPalabra = texto;
        rutaAudio = audio;
        traduccionFicha = traduccion;
        indiceEnTablero = indicePos; // 👈 Unificado correctamente aquí

        estaEliminada = false;

        if (fondoVisual != null) fondoVisual.SetActive(true);
        OcultarFicha();
    }

    public void AlHacerClic()
    {
        if (estaEliminada || estaVolteada) return;

        Debug.Log("¡CLIC EXITOSO EN LA FICHA: " + textoPalabra + " (Índice: " + indiceEnTablero + ")!");

        RevelarFicha();

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

            if (ControladorJuego.Instance.socket != null)
            {
                ControladorJuego.Instance.socket.Emit("procesar_clic_ficha", datosClic);
            }
        }

        if (GestorTablero.Instance != null)
        {
            GestorTablero.Instance.FichaSeleccionada(this);
        }
    }

    public void RevelarFicha()
    {
        estaVolteada = true;
        if (textoFicha != null)
        {
            textoFicha.text = textoPalabra;
        }

        if (fondoVisual != null)
        {
            fondoVisual.SetActive(false);
        }
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
        // Esto se usa exclusivamente cuando fallan y vuelven a quedar boca abajo
        if (estaEliminada) return; // Si ya fue encontrada, no la toques

        estaVolteada = false;

        if (textoFicha != null)
        {
            textoFicha.text = "";
        }

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

        if (textoFicha != null)
            textoFicha.text = "";

        if (fondoVisual != null)
            fondoVisual.SetActive(false);

        // Apagamos el objeto por completo para que desaparezca de la matriz del juego
       // gameObject.SetActive(false);
    }


}