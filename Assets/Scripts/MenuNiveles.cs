using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menú hamburguesa para seleccionar niveles. Diseñado visualmente en Unity
/// usando un Prefab y un Grid Layout Group en el contenedor.
/// </summary>
public class MenuNiveles : MonoBehaviour
{
    public const string ClaveNivelSeleccionado = "NivelSeleccionado";
    public GameObject panelNivel;
    public Transform contenedorBotones;

    [Header("Botones asignados desde Unity")]
    public Button botonHamburguesa;
    public Button botonMaquina;
    public Button botonPracticaIndividual;

    public GameObject prefabBotonNivel;
    public int ultimoNivelDisponible = 6;
    public GameObject panelLobby;
    public GameObject panelLogin;

    private bool menuAbierto;
    public static MenuNiveles Instance;

    private void Start()
    {
        ultimoNivelDisponible = Mathf.Max(1, ultimoNivelDisponible);

        if (panelNivel == null)
            panelNivel = BuscarObjeto("PanelNivel");

        if (panelLogin == null)
            panelLogin = BuscarObjeto("Panel_Login");

        // Configurar el botón de hamburguesa si fue asignado en el Inspector
        if (botonHamburguesa != null)
        {
            botonHamburguesa.onClick.RemoveListener(alternarMenu);
            botonHamburguesa.onClick.AddListener(alternarMenu);
        }

        // Configurar el botón de jugar con máquina si fue asignado en el Inspector
        if (botonMaquina != null)
        {
            botonMaquina.onClick.RemoveListener(AbrirMenuDificultad);
            botonMaquina.onClick.AddListener(AbrirMenuDificultad);
        }

        // Configurar el botón de práctica individual si fue asignado en el Inspector
        if (botonPracticaIndividual == null)
        {
            ControladorModoSolo modoSolo = FindFirstObjectByType<ControladorModoSolo>();
            if (modoSolo != null) botonPracticaIndividual = modoSolo.botonModoSolo;
        }

        if (botonPracticaIndividual != null)
        {
            botonPracticaIndividual.onClick.RemoveListener(SolicitarPracticaIndividual);
            botonPracticaIndividual.onClick.AddListener(SolicitarPracticaIndividual);
        }

        ReconstruirBotones();
        ActualizarVisibilidad();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        ActualizarVisibilidad();
    }

    private void ActualizarVisibilidad()
    {
        // Aquí puedes descomentar o ajustar la lógica de visibilidad según tu panel de lobby
    }

    private void SolicitarPracticaIndividual()
    {
        ControladorModoSolo modoSolo = FindFirstObjectByType<ControladorModoSolo>();
        if (modoSolo != null)
            modoSolo.SolicitarModoIndividual();
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

    private void alternarMenu()
    {
        menuAbierto = !menuAbierto;
       // if (panelNivel != null) panelNivel.SetActive(menuAbierto);
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

        // Limpiamos los botones anteriores que existan en el contenedor
        for (int i = contenedorBotones.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedorBotones.GetChild(i);
            Destroy(hijo.gameObject);
        }

        // Instanciamos los botones de niveles basados en el Prefab visual de Unity
        for (int nivel = 1; nivel <= ultimoNivelDisponible; nivel++)
        {
            if (prefabBotonNivel == null)
            {
                Debug.LogError("Falta asignar el 'Prefab Boton Nivel' en el Inspector de Unity.");
                break;
            }

            GameObject objetoInstanciado = Instantiate(prefabBotonNivel, contenedorBotones);
            Button boton = objetoInstanciado.GetComponentInChildren<Button>(); // <-- Usamos esto para que busque al hijo            boton.name = "Nivel_" + nivel;
            int nivelSeleccionado = nivel;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => SeleccionarNivel(nivelSeleccionado));

            TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto != null) texto.text = "Nivel " + nivel;
        }
    }

    public void SeleccionarNivel(int nivel)
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