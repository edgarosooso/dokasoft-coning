using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menú hamburguesa para seleccionar niveles. Añádelo al objeto Gestor o al Canvas
/// y asigna PanelNivel en el Inspector. El servidor puede actualizar el límite con
/// ConfigurarUltimoNivel().
/// </summary>
public class MenuNiveles : MonoBehaviour
{
    public const string ClaveNivelSeleccionado = "NivelSeleccionado";
    public GameObject panelNivel;
    public Transform contenedorBotones;
    public Button botonHamburguesa;
    public Button botonMaquina;
    public Button botonPracticaIndividual;
    public Button prefabBotonNivel;
    public int ultimoNivelDisponible = 100;
    public GameObject panelLobby;
    public GameObject panelLogin;

    private bool menuAbierto;

    private void Start()
    {
        ultimoNivelDisponible = Mathf.Max(1, ultimoNivelDisponible);
        if (panelNivel == null)
            panelNivel = BuscarObjeto("PanelNivel");

        if (panelNivel != null)
            panelNivel.SetActive(false);

        if (panelLobby == null) panelLobby = BuscarObjeto("Panel_Lobby");
        if (panelLogin == null) panelLogin = BuscarObjeto("Panel_Login");

        if (botonPracticaIndividual == null)
        {
            ControladorModoSolo modoSolo = FindFirstObjectByType<ControladorModoSolo>();
            if (modoSolo != null) botonPracticaIndividual = modoSolo.botonModoSolo;
        }
        // La opción se mostrará dentro del menú de niveles, no como botón separado.
        if (botonPracticaIndividual != null)
            botonPracticaIndividual.gameObject.SetActive(false);

        CrearBotonHamburguesaSiFalta();
        CrearBotonMaquinaSiFalta();
        CrearBotonPracticaSiFalta();
        ReconstruirBotones();
        ActualizarVisibilidad();
    }

    private void Update()
    {
        ActualizarVisibilidad();
    }

    private void ActualizarVisibilidad()
    {
        bool mostrarEnLobby = panelLobby != null && panelLobby.activeInHierarchy &&
                              (panelLogin == null || !panelLogin.activeInHierarchy);

        if (botonHamburguesa != null)
            botonHamburguesa.gameObject.SetActive(mostrarEnLobby);

        if (botonMaquina != null)
            botonMaquina.gameObject.SetActive(mostrarEnLobby);

        if (botonPracticaIndividual != null)
            botonPracticaIndividual.gameObject.SetActive(mostrarEnLobby);

        if (!mostrarEnLobby)
        {
            menuAbierto = false;
            if (panelNivel != null) panelNivel.SetActive(false);
        }
    }

    private void CrearBotonHamburguesaSiFalta()
    {
        if (botonHamburguesa != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject objeto = new GameObject("BtnMenuNiveles", typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(canvas.transform, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(90f, 90f);

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = new Color(0.04f, 0.18f, 0.45f, 0.95f);
        botonHamburguesa = objeto.GetComponent<Button>();
        botonHamburguesa.onClick.AddListener(alternarMenu);

        GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(objeto.transform, false);
        TextMeshProUGUI texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.text = "☰";
        texto.fontSize = 48f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.sizeDelta = Vector2.zero;
    }

    private void CrearBotonMaquinaSiFalta()
    {
        if (botonMaquina != null)
        {
            botonMaquina.onClick.RemoveListener(AbrirMenuDificultad);
            botonMaquina.onClick.AddListener(AbrirMenuDificultad);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject objeto = new GameObject("BtnJugarMaquina", typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(canvas.transform, false);

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -108f);
        rect.sizeDelta = new Vector2(420f, 72f);

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = new Color(0.08f, 0.35f, 0.8f, 0.95f);
        botonMaquina = objeto.GetComponent<Button>();
        botonMaquina.onClick.AddListener(AbrirMenuDificultad);

        GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(objeto.transform, false);
        TextMeshProUGUI texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.text = "Jugar con la máquina";
        texto.fontSize = 32f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.sizeDelta = Vector2.zero;
    }

    private void CrearBotonPracticaSiFalta()
    {
        // El botón original se mantiene oculto; creamos uno junto al de máquina.
        if (botonPracticaIndividual != null && botonPracticaIndividual.gameObject.name == "BtnPracticaIndividual")
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (botonPracticaIndividual != null)
            botonPracticaIndividual.gameObject.SetActive(false);

        GameObject objeto = new GameObject("BtnPracticaIndividual", typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(canvas.transform, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -190f);
        rect.sizeDelta = new Vector2(420f, 72f);

        objeto.GetComponent<Image>().color = new Color(0.08f, 0.35f, 0.8f, 0.95f);
        botonPracticaIndividual = objeto.GetComponent<Button>();
        botonPracticaIndividual.onClick.AddListener(() =>
        {
            ControladorModoSolo modoSolo = FindFirstObjectByType<ControladorModoSolo>();
            if (modoSolo != null) modoSolo.SolicitarModoIndividual();
        });

        GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(objeto.transform, false);
        TextMeshProUGUI texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.text = "Práctica individual";
        texto.fontSize = 32f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.sizeDelta = Vector2.zero;
    }

    private void AbrirMenuDificultad()
    {
        menuAbierto = true;
        if (panelNivel != null) panelNivel.SetActive(true);
    }

    private void JugarConMaquina(int dificultad)
    {
        int nivel = PlayerPrefs.GetInt(ClaveNivelSeleccionado, 1);
        if (GestorTablero.Instance != null)
            GestorTablero.Instance.nivelActual = nivel;

        ControladorModoSolo modoSolo = FindFirstObjectByType<ControladorModoSolo>();
        if (modoSolo != null)
            modoSolo.SolicitarModoMaquina(dificultad);
        else
            Debug.LogError("No se encontró ControladorModoSolo para iniciar la partida contra la máquina.");
    }

    // Separado para que Unity pueda registrar correctamente el listener.
    private void alternarMenu()
    {
        menuAbierto = !menuAbierto;
        if (panelNivel != null) panelNivel.SetActive(menuAbierto);
    }

    public void ConfigurarUltimoNivel(int ultimoNivel)
    {
        ultimoNivelDisponible = Mathf.Max(1, ultimoNivel);
        ReconstruirBotones();
    }

    private void ReconstruirBotones()
    {
        if (panelNivel == null) return;
        if (contenedorBotones == null) contenedorBotones = panelNivel.transform;

        RectTransform rectContenedor = contenedorBotones as RectTransform;
        if (rectContenedor != null)
        {
            rectContenedor.anchorMin = rectContenedor.anchorMax = new Vector2(0.5f, 1f);
            rectContenedor.pivot = new Vector2(0.5f, 1f);
            rectContenedor.anchoredPosition = new Vector2(0f, -20f);
            rectContenedor.sizeDelta = new Vector2(360f, Mathf.Max(430f, ultimoNivelDisponible * 64f));
        }

        ScrollRect scroll = panelNivel.GetComponent<ScrollRect>();
        if (scroll == null) scroll = panelNivel.AddComponent<ScrollRect>();
        scroll.viewport = panelNivel.GetComponent<RectTransform>();
        scroll.content = rectContenedor;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        if (panelNivel.GetComponent<RectMask2D>() == null)
            panelNivel.AddComponent<RectMask2D>();

        VerticalLayoutGroup layout = contenedorBotones.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = contenedorBotones.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = contenedorBotones.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedorBotones.GetChild(i);
            Destroy(hijo.gameObject);
        }

        // Las dificultades quedan al principio del menú para encontrarlas sin
        // tener que desplazarse por todos los niveles.
        CrearBotonesDificultadMaquina();

        for (int nivel = 1; nivel <= ultimoNivelDisponible; nivel++)
        {
            Button boton = prefabBotonNivel != null
                ? Instantiate(prefabBotonNivel, contenedorBotones)
                : CrearBotonNivel(contenedorBotones);
            boton.name = "Nivel_" + nivel;
            int nivelSeleccionado = nivel;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => SeleccionarNivel(nivelSeleccionado));
            TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto != null) texto.text = "Nivel " + nivel;
        }

        CrearSelectorEscrito();
    }

    private void CrearBotonesDificultadMaquina()
    {
        CrearBotonDificultad("Fácil", 1);
        CrearBotonDificultad("Medio", 2);
        CrearBotonDificultad("Difícil", 3);
    }

    private void CrearBotonDificultad(string nombre, int dificultad)
    {
        Button boton = CrearBotonNivel(contenedorBotones);
        boton.name = "BtnMaquina" + nombre;
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(() => JugarConMaquina(dificultad));
        TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (texto != null) texto.text = "Máquina: " + nombre;
    }

    private void CrearSelectorEscrito()
    {
        GameObject objeto = new GameObject("EntradaNivel", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        objeto.transform.SetParent(panelNivel.transform, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -260f);
        rect.sizeDelta = new Vector2(260f, 52f);
        objeto.GetComponent<Image>().color = Color.white;

        TMP_InputField entrada = objeto.GetComponent<TMP_InputField>();
        entrada.contentType = TMP_InputField.ContentType.IntegerNumber;
        entrada.characterLimit = 2;
        entrada.text = "";

        GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(objeto.transform, false);
        TextMeshProUGUI texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.fontSize = 22f;
        texto.color = Color.black;
        texto.alignment = TextAlignmentOptions.Center;
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.sizeDelta = Vector2.zero;
        entrada.textComponent = texto;

        GameObject placeholderObjeto = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObjeto.transform.SetParent(objeto.transform, false);
        TextMeshProUGUI placeholder = placeholderObjeto.GetComponent<TextMeshProUGUI>();
        placeholder.text = $"Escribe nivel (1-{ultimoNivelDisponible})";
        placeholder.fontSize = 18f;
        placeholder.color = Color.gray;
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.rectTransform.anchorMin = Vector2.zero;
        placeholder.rectTransform.anchorMax = Vector2.one;
        placeholder.rectTransform.sizeDelta = Vector2.zero;
        entrada.placeholder = placeholder;

        entrada.onEndEdit.AddListener(SeleccionarNivelEscrito);
    }

    private void SeleccionarNivelEscrito(string valor)
    {
        if (!int.TryParse(valor, out int nivel)) return;
        if (nivel < 1 || nivel > ultimoNivelDisponible)
        {
            Debug.LogWarning($"El nivel debe estar entre 1 y {ultimoNivelDisponible}.");
            return;
        }
        SeleccionarNivel(nivel);
    }

    private Button CrearBotonNivel(Transform padre)
    {
        GameObject objeto = new GameObject("Nivel", typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(padre, false);
        objeto.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 56f);
        objeto.GetComponent<Image>().color = new Color(0.08f, 0.35f, 0.8f, 0.95f);
        GameObject textoObjeto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(objeto.transform, false);
        TextMeshProUGUI texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.fontSize = 24f;
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.sizeDelta = Vector2.zero;
        return objeto.GetComponent<Button>();
    }

    private void SeleccionarNivel(int nivel)
    {
        PlayerPrefs.SetInt(ClaveNivelSeleccionado, nivel);
        PlayerPrefs.Save();
        menuAbierto = false;
        if (panelNivel != null) panelNivel.SetActive(false);
        if (GestorTablero.Instance != null)
            GestorTablero.Instance.nivelActual = nivel;
        Debug.Log("Nivel guardado para la próxima partida: " + nivel);
    }

    private GameObject BuscarObjeto(string nombre)
    {
        foreach (GameObject objeto in Resources.FindObjectsOfTypeAll<GameObject>())
            if (objeto.name == nombre && objeto.scene.IsValid()) return objeto;
        return null;
    }
}
