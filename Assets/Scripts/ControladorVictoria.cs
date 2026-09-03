using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

[ExecuteAlways]
public class ControladorVictoria : MonoBehaviour
{
    [Header("Textos de la UI de Victoria")]
    public TextMeshProUGUI txtNivelCompletado;      // Texto principal de nivel
    public TextMeshProUGUI txtNombreJugadorX;       // Nombre del jugador X
    public TextMeshProUGUI txtPuntosJugadorX;       // Puntos o parejas de X
    public TextMeshProUGUI txtNombreJugadorY;       // Nombre del jugador Y
    public TextMeshProUGUI txtPuntosJugadorY;       // Puntos o parejas de Y
    public TextMeshProUGUI txtMensajeGanador;       // Ej: "Edgar Oso DOMINA EL TABLERO"
    public TextMeshProUGUI txtBotonSiguiente;       // Texto del botón de avanzar
    public TextMeshProUGUI txtContadorSiguiente;    // Número de la cuenta regresiva (ej. 10)

    private bool partidaFinalizada = false;
    private string nombreSalaActual;
    private int nivelSiguiente;
    public RawImage imagenAvatarX;
    public RawImage imagenAvatarY;

    private void Awake()
    {
        // La escena puede no tener todavía todos los textos configurados en el Inspector.
        // Se crean automáticamente para que la pantalla nunca aparezca vacía.
        txtNivelCompletado = txtNivelCompletado ?? CrearTexto("TxtNivelVictoria", new Vector2(0, 330), 46);
        txtNombreJugadorX = txtNombreJugadorX ?? CrearTexto("TxtNombreJugadorX", new Vector2(-520, 170), 30);
        txtPuntosJugadorX = txtPuntosJugadorX ?? CrearTexto("TxtPuntosJugadorX", new Vector2(-520, -170), 28);
        txtNombreJugadorY = txtNombreJugadorY ?? CrearTexto("TxtNombreJugadorY", new Vector2(520, 170), 30);
        txtPuntosJugadorY = txtPuntosJugadorY ?? CrearTexto("TxtPuntosJugadorY", new Vector2(520, -170), 28);
        txtMensajeGanador = txtMensajeGanador ?? CrearTexto("TxtGanador", new Vector2(0, 0), 38);
        txtContadorSiguiente = txtContadorSiguiente ?? CrearContadorEnBoton();
        // Avatar X: lado izquierdo y centrado verticalmente del panel.
        imagenAvatarX = imagenAvatarX ?? CrearAvatar("ImagenAvatarX", new Vector2(-520, 0));
        imagenAvatarY = imagenAvatarY ?? CrearAvatar("ImagenAvatarY", new Vector2(520, 0));
    }

    private void OnEnable()
    {
        // El panel empieza desactivado en la escena; garantizamos las referencias al activarlo.
        if (txtNivelCompletado == null) Awake();
    }

    private TextMeshProUGUI CrearTexto(string nombre, Vector2 posicion, float tamano)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(transform, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(700, 90);
        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        texto.fontSize = tamano;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        return texto;
    }

    private RawImage CrearAvatar(string nombre, Vector2 posicion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        objeto.transform.SetParent(transform, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(250, 250);
        return objeto.GetComponent<RawImage>();
    }

    private TextMeshProUGUI CrearContadorEnBoton()
    {
        Transform boton = transform.Find("BtnSiguiente");
        if (boton == null)
            return CrearTexto("TxtContadorSiguiente", new Vector2(400, -280), 30);

        GameObject objeto = new GameObject("TxtContadorSiguiente", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(boton, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-55f, 0f);
        rect.sizeDelta = new Vector2(80f, 80f);
        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        texto.fontSize = 30f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.yellow;
        texto.raycastTarget = false;
        return texto;
    }

    public void ActivarPantallaVictoria(int nivelActual, string sala, string nombreJugadorX = null, int puntosX = 0, string nombreJugadorYParam = null, int puntosY = 0)
    {
        gameObject.SetActive(true);
        partidaFinalizada = true;
        nombreSalaActual = sala;
        nivelSiguiente = nivelActual + 1;

        bool esModoMultijugador = ControladorJuego.Instance != null &&
                                   ControladorJuego.Instance.esModoMultijugador;
        ConfigurarVisibilidadModo(esModoMultijugador);

        // 1. Actualizar textos de nivel y botón
        if (txtNivelCompletado != null) txtNivelCompletado.text = $"Nivel {nivelActual} Completado";
        if (txtBotonSiguiente != null) txtBotonSiguiente.text = $"Avanzar al Nivel {nivelSiguiente}";

        // 2. Nombres y puntajes
        string nombreY = "Jugador Y";

        string nombreX = string.IsNullOrWhiteSpace(nombreJugadorX) ? "Jugador X" : nombreJugadorX;
        nombreY = string.IsNullOrWhiteSpace(nombreJugadorYParam) ? "Jugador Y" : nombreJugadorYParam;

        if (txtNombreJugadorX != null) txtNombreJugadorX.text = nombreX;
        if (txtNombreJugadorY != null) txtNombreJugadorY.text = nombreY;

        // Forzamos la asignación de texto para ambos jugadores para evitar que queden textos fijos antiguos
        if (txtPuntosJugadorX != null) txtPuntosJugadorX.text = $"Pts {puntosX}";
        if (txtPuntosJugadorY != null) txtPuntosJugadorY.text = $"Pts {puntosY}";

        // 3. Mensaje del ganador
        if (txtMensajeGanador != null)
        {
            if (!esModoMultijugador)
                txtMensajeGanador.text = "PRÁCTICA COMPLETADA";
            else
            {
                string ganador = puntosX == puntosY ? "Empate" : (puntosX > puntosY ? nombreX : nombreY);
                txtMensajeGanador.text = ganador == "Empate" ? "EMPATE" : $"{ganador} DOMINA EL TABLERO";
            }
        }

        if (txtContadorSiguiente != null) txtContadorSiguiente.gameObject.SetActive(false);
        CargarAvatarJugador(nombreX, imagenAvatarX);
        if (esModoMultijugador)
            CargarAvatarJugador(nombreY, imagenAvatarY);
    }

    private void ConfigurarVisibilidadModo(bool esModoMultijugador)
    {
        SetActivo(txtNombreJugadorY, esModoMultijugador);
        SetActivo(txtPuntosJugadorY, esModoMultijugador);
        SetActivo(imagenAvatarY, esModoMultijugador);
    }

    private void SetActivo(Component componente, bool activo)
    {
        if (componente != null) componente.gameObject.SetActive(activo);
    }

    private void CargarAvatarJugador(string nombre, RawImage destino)
    {
        string url = "";
        if (ControladorJuego.Instance != null)
        {
            ControladorJuego.Instance.avataresPorJugador.TryGetValue(nombre, out url);
            if (string.IsNullOrEmpty(url)) url = ControladorJuego.Instance.avatar_url;
        }
        url = NormalizarUrlAvatar(url);
        if (destino == null)
        {
            Debug.LogWarning($"No hay RawImage asignado para el avatar de {nombre}.");
            return;
        }
        if (!string.IsNullOrEmpty(url))
        {
            StartCoroutine(DescargarAvatar(url, destino));
            // Mientras el servidor no envíe un avatar separado para Y, usamos el mismo como respaldo.
            
        }
    }

    private string NormalizarUrlAvatar(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim();
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        if (url.StartsWith("/")) return "https://dokasoft.com" + url;
        return "https://dokasoft.com/dokasoft-coning/" + url.TrimStart('/');
    }

    private void ConfigurarContadorEnBoton()
    {
        Transform boton = transform.Find("BtnSiguiente");
        if (boton == null)
        {
            foreach (Transform hijo in GetComponentsInChildren<Transform>(true))
            {
                if (hijo.name == "BtnSiguiente") { boton = hijo; break; }
            }
        }
        if (boton == null || txtContadorSiguiente == null) return;

        txtContadorSiguiente.transform.SetParent(boton, false);
        RectTransform rect = txtContadorSiguiente.rectTransform;
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-45f, 0f);
        rect.sizeDelta = new Vector2(70f, 70f);
        txtContadorSiguiente.fontSize = 52f;
        txtContadorSiguiente.color = Color.yellow;
        txtContadorSiguiente.raycastTarget = false;

        Button button = boton.GetComponent<Button>();
        Image image = boton.GetComponent<Image>();
        if (button == null) button = boton.gameObject.AddComponent<Button>();
        if (image == null) image = boton.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.35f, 0.8f, 0.95f);
        button.targetGraphic = image;
    }

    private IEnumerator DescargarAvatar(string url, RawImage destino)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success && destino != null)
                destino.texture = DownloadHandlerTexture.GetContent(request);
            else if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"No se pudo cargar el avatar de victoria desde '{url}': {request.error}");
        }
    }

    public void OnClickSiguienteNivel()
    {
        PararPartida();
        EjecutarSiguienteNivel();
    }

    void PararPartida()
    {
        partidaFinalizada = false;
        StopAllCoroutines();
    }

    public void DetenerTemporizador()
    {
        PararPartida();
    }

    void EjecutarSiguienteNivel()
    {
        PararPartida();
        if (GestorTablero.Instance != null)
        {
            // Usamos el mismo flujo del botón para actualizar nivel y notificar a ambos jugadores.
            GestorTablero.Instance.BotonSiguienteNivel_Click();
        }
    }
}
