using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorFicha : MonoBehaviour
{
    private Button botonFicha;
    private TextMeshProUGUI textoFicha;

    [Header("Referencias Visuales")]
    [Tooltip("Arrastra aquí el objeto hijo 'FondoVisual' desde el inspector de Unity")]
    public GameObject fondoVisual; // 👈 Referencia a la cajita de neón

    public int idFicha;
    public string textoPalabra;
    public string rutaAudio;

    private bool estaVolteada = false;
    private bool estaEliminada = false; // Para evitar clics si ya se hizo pareja

    void Awake()
    {
        botonFicha = GetComponent<Button>();

        // Si no asignaste el fondo visual a mano, lo busca automáticamente en los hijos
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

    public void ConfigurarFicha(int id, string texto, string audio)
    {
        idFicha = id;
        textoPalabra = texto;
        rutaAudio = audio;
        estaEliminada = false;

        if (fondoVisual != null) fondoVisual.SetActive(true); // Asegura que se vea al reiniciar
        OcultarFicha();
    }

    public void AlHacerClic()
    {
        if (estaEliminada || estaVolteada) return;

        Debug.Log("¡CLIC EXITOSO EN LA FICHA: " + textoPalabra + "!");

        RevelarFicha();

        if (!string.IsNullOrEmpty(rutaAudio))
        {
            GestorTablero.Instance.ReproducirAudioDeFila(rutaAudio);
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

        // Oculta el cuadro al revelar la ficha
        if (fondoVisual != null)
        {
            fondoVisual.SetActive(false);
        }
    }

   public void OcultarFicha()
{
    estaVolteada = false;
    
    if (textoFicha != null)
    {
        textoFicha.text = "";
    }

    if (fondoVisual != null)
    {
        fondoVisual.SetActive(true); // Vuelve a mostrar el cuadro con seguridad
    }
}

    // 🌟 NUEVO MÉTODO: Llamar a esto desde tu GestorTablero cuando hagan pareja con éxito
    public void MarcarComoEncontrada()
    {
        estaEliminada = true;

        if (botonFicha != null)
            botonFicha.interactable = false; // Desactiva el botón para que no se pueda volver a presionar

        if (textoFicha != null)
            textoFicha.text = ""; // Limpia el texto

        if (fondoVisual != null)
            fondoVisual.SetActive(false); // 👈 ¡Aquí se oculta limpiamente la cajita de neón!
    }
}