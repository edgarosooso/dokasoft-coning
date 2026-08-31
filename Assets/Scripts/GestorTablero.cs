using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GestorTablero : MonoBehaviour
{
    [Header("UI de Turnos")]
    public TextMeshProUGUI textoTurnoUI;

    [Header("Panel de Traducción")]
    public GameObject panelTraduccionObjeto;
    public TextMeshProUGUI textoTraduccionPanel;
    public float tiempoVisiblePanel = 4f;

    public static GestorTablero Instance;
    public int nivelActual = 1;
    public TextMeshProUGUI textoNivelTitulo;

    [Header("UI Traducción")]
    public TextMeshProUGUI textoTraduccionUI;
    private string traduccionActual = "";
    private bool estaRevelado = false;

    [Header("Referencias de Victoria / Siguiente Nivel")]
    public GameObject panelNivelCompletado;
    private int parejasEncontradas = 0;
    private int totalParejasDelNivel = 0;

    [Header("Referencias de Paneles para Navegación")]
    public GameObject panelJuego;
    public GameObject panelLobby;

    [Header("Referencias de UI")]
    public Transform contenedorMatriz;
    public GameObject prefabCuadro;

    private bool bloqueandoClics = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ActualizarTextoTurno();
    }





    public void OcultarFichasLocales(int indice1, int indice2)
    {
        Debug.Log($"🚨 ENTRÓ A OcultarFichasLocales -> Indice1: {indice1}, Indice2: {indice2}");

        if (contenedorMatriz == null)
        {
            Debug.LogWarning("⚠️ contenedorMatriz es nulo en OcultarFichasLocales");
            return;
        }

        // Obtenemos todas las fichas hijas del contenedor
        ControladorFicha[] todasLasFichas = contenedorMatriz.GetComponentsInChildren<ControladorFicha>(true);

        bool ficha1Encontrada = false;
        bool ficha2Encontrada = false;

        foreach (var ficha in todasLasFichas)
        {
            // Comparamos contra el índice lógico de la base de datos, no contra el orden visual
            if (ficha.indiceEnTablero == indice1)
            {
                // 🛡️ Blindaje: Si ya fue eliminada por un acierto previo, no la toques
                if (!ficha.estaEliminada)
                {
                    Debug.Log($"✅ Ocultando ficha 1 con índice lógico {indice1} (Texto: {ficha.textoPalabra})");
                    ficha.OcultarFicha();
                }
                ficha1Encontrada = true;
            }
            else if (ficha.indiceEnTablero == indice2)
            {
                // 🛡️ Blindaje: Si ya fue eliminada por un acierto previo, no la toques
                if (!ficha.estaEliminada)
                {
                    Debug.Log($"✅ Ocultando ficha 2 con índice lógico {indice2} (Texto: {ficha.textoPalabra})");
                    ficha.OcultarFicha();
                }
                ficha2Encontrada = true;
            }

            // Si ya encontramos ambas, podemos salir del bucle anticipadamente
            if (ficha1Encontrada && ficha2Encontrada) break;
        }

        if (!ficha1Encontrada) Debug.LogWarning($"⚠️ No se encontró ninguna ficha con el índice lógico {indice1}");
        if (!ficha2Encontrada) Debug.LogWarning($"⚠️ No se encontró ninguna ficha con el índice lógico {indice2}");
    }






    public void ActualizarTurnoUI(int nuevoTurnoId)
    {
        if (textoTurnoUI == null || ControladorJuego.Instance == null) return;

        if (ControladorJuego.Instance.id_player == nuevoTurnoId)
        {
            textoTurnoUI.text = "Tu Turno";
        }
        else
        {
            textoTurnoUI.text = "Turno del Rival";
        }
    }


    public void ActualizarTextoTurno()
    {
        if (textoTurnoUI == null || ControladorJuego.Instance == null) return;

        bool esMultijugadorReal = ControladorJuego.Instance.esModoMultijugador &&
                                 ControladorJuego.Instance.socket != null &&
                                 !string.IsNullOrEmpty(ControladorJuego.Instance.nombreSalaActual);

        if (!esMultijugadorReal)
        {
            textoTurnoUI.text = "Modo Solo";
            return;
        }

        // Si el servidor envía un número de turno (ej: 0 o 1) y en ControladorJuego guardas tu índice de jugador local:
        // (O si el servidor te manda directamente si es tu turno con un booleano o índice)
        string turnoServidorStr = ControladorJuego.Instance.turnoActual;

        Debug.Log($"🔎 Evaluando turnos -> Turno del servidor recibido: [{turnoServidorStr}]");

        // Si tu índice local es el 0 y el servidor manda "0", o si coincide con tu posición en la sala:
        // Comparamos directamente contra el índice del jugador guardado localmente:
        int.TryParse(turnoServidorStr, out int turnoServidorInt);

        // Suponiendo que guardas tu índice de jugador en ControladorJuego.Instance.indiceJugadorLocal (ej: 0 o 1):
        if (turnoServidorInt == ControladorJuego.Instance.id_player)
        {
            textoTurnoUI.text = "Tu Turno";
        }
        else
        {
            // O si prefieres una validación directa por texto si el servidor manda el identificador numérico de otra forma:
            textoTurnoUI.text = "Turno del Rival";
        }
    }


    public void ConfigurarTablero(List<ItemNivelRespuesta> listaFichas)
    {
        if (listaFichas == null) return;
        if (contenedorMatriz == null || prefabCuadro == null) return;

        foreach (Transform child in contenedorMatriz)
        {
            Destroy(child.gameObject);
        }

        bloqueandoClics = false;
        parejasEncontradas = 0;
        totalParejasDelNivel = listaFichas.Count / 2;

        if (panelNivelCompletado != null)
            panelNivelCompletado.SetActive(false);

        for (int i = 0; i < listaFichas.Count; i++)
        {
            var ficha = listaFichas[i];
            GameObject nuevoCuadro = Instantiate(prefabCuadro, contenedorMatriz);
            nuevoCuadro.transform.localScale = Vector3.one;

            ControladorFicha controladorFicha = nuevoCuadro.GetComponent<ControladorFicha>();
            if (controladorFicha != null)
            {
                // Configura la ficha pasándole el índice que viene de la base de datos/servidor
                controladorFicha.ConfigurarFicha(ficha.id, ficha.texto, ficha.audio, ficha.traduccion, ficha.indice_posicion);

                // 🔍 IMPRESIÓN DE DEPURACIÓN: Vamos a ver en consola si el índice se asignó bien
                Debug.Log($"📌 Ficha instanciada -> Texto: {ficha.texto} | IndicePosicion asignado: {ficha.indice_posicion}");
            }
        }
        ActualizarTextoTurno();
    }
    public void FichaSeleccionada(ControladorFicha ficha)
    {
        if (bloqueandoClics || ficha == null) return;

        bool esMultijugadorReal = (ControladorJuego.Instance != null &&
                                   ControladorJuego.Instance.esModoMultijugador &&
                                   ControladorJuego.Instance.socket != null &&
                                   !string.IsNullOrEmpty(ControladorJuego.Instance.nombreSalaActual));

        if (esMultijugadorReal)
        {
            string miSocketId = ControladorJuego.Instance.socket.Id;

            if (ControladorJuego.Instance.turnoActual != miSocketId)
            {
                return;
            }
        }

        ficha.RevelarFicha();

        if (esMultijugadorReal)
        {
            int indiceFicha = ficha.indiceEnTablero;

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["nombreSala"] = ControladorJuego.Instance.nombreSalaActual;
            data["indiceFicha"] = indiceFicha;
            data["socketId"] = ControladorJuego.Instance.socket.Id;

            ControladorJuego.Instance.socket.Emit("voltear_ficha_servidor", data);
            bloqueandoClics = true;
        }

        if (textoTraduccionUI != null)
        {
            traduccionActual = ficha.traduccionFicha;
            textoTraduccionUI.text = "[ Toca para ver traducción ]";
            estaRevelado = false;
        }
    }

    private System.Collections.IEnumerator MostrarYQuitarPanelCo(string palabra, string rutaAudio)
    {
        if (textoTraduccionPanel != null) textoTraduccionPanel.text = palabra;
        if (panelTraduccionObjeto != null) panelTraduccionObjeto.SetActive(true);

        if (!string.IsNullOrEmpty(rutaAudio)) ReproducirAudioDeFila(rutaAudio);

        yield return new WaitForSeconds(tiempoVisiblePanel);

        if (panelTraduccionObjeto != null) panelTraduccionObjeto.SetActive(false);
    }

    private System.Collections.IEnumerator MostrarAvisoNivelCompletadoCo()
    {
        yield return new WaitForSeconds(0.5f);
        if (panelJuego != null) panelJuego.SetActive(false);
        if (panelNivelCompletado != null) panelNivelCompletado.SetActive(true);
    }


    private void DesactivarFichaAcierto(ControladorFicha ficha)
    {
        if (ficha != null)
        {
            ficha.MarcarComoEncontrada();
            // Opcional: Si quieres que el objeto GameObject se apague para liberar espacio visual sin romper la jerarquía,
            // podemos retrasarlo un mini frame o simplemente dejar que MarcarComoEncontrada desactive su imagen y botón.
        }
    }


    public void ReproducirAudioDeFila(string rutaAudioRelativa)
    {
        rutaAudioRelativa = rutaAudioRelativa.TrimStart('/');
        string urlAudio = $"{AppConfig.BaseURL.Replace("/api", "")}/{rutaAudioRelativa}";
        StartCoroutine(DescargarYReproducirAudio(urlAudio));
    }

    private System.Collections.IEnumerator DescargarYReproducirAudio(string url)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    AudioSource audioSource = GetComponent<AudioSource>();
                    if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.PlayOneShot(clip);
                }
            }
        }
    }

    public void BotonSiguienteNivel_Click()
    {
        if (panelNivelCompletado != null) panelNivelCompletado.SetActive(false);
        nivelActual++;
        if (textoNivelTitulo != null) textoNivelTitulo.text = "Nivel " + nivelActual;
        if (panelJuego != null) panelJuego.SetActive(true);

        FindFirstObjectByType<ControladorModoSolo>().SolicitarModoIndividual();
    }

    public void BotonMenuPrincipal_Click()
    {
        if (panelNivelCompletado != null) panelNivelCompletado.SetActive(false);
        if (panelJuego != null) panelJuego.SetActive(false);
        if (panelLobby != null) panelLobby.SetActive(true);
    }

    // 🌟 Método requerido por ControladorModoSolo, ControladorInvitaciones y ControladorModoPareja
    public void CargarNuevaPalabra(string traduccionEspanol)
    {
        traduccionActual = traduccionEspanol;
        estaRevelado = false;

        if (textoTraduccionUI != null)
        {
            textoTraduccionUI.text = "[ Toca para ver traducción ]";
        }
    }

    public void AlHacerClicEnTraduccion()
    {
        if (textoTraduccionUI == null) return;

        if (!estaRevelado)
        {
            textoTraduccionUI.text = traduccionActual;
            estaRevelado = true;
        }
        else
        {
            textoTraduccionUI.text = "[ Toca para ver traducción ]";
            estaRevelado = false;
        }
    }

    // Método 1: Habilita los clics de nuevo tras fallar una pareja
    public void HabilitarClicsDespuesDeFallar()
    {
        bloqueandoClics = false;
    }

    // Método 2: Revela una ficha presionada por el rival
    // Método 2: Revela una ficha presionada por el rival buscando por su índice lógico
    public void RevelarFichaRemota(int indice)
    {
        if (contenedorMatriz == null) return;

        ControladorFicha[] todasLasFichas = contenedorMatriz.GetComponentsInChildren<ControladorFicha>(true);

        foreach (var ficha in todasLasFichas)
        {
            if (ficha.indiceEnTablero == indice)
            {
                ficha.RevelarFicha();
                break;
            }
        }
    }

    // Método 3: Procesa el acierto de una pareja a nivel global



    public void ProcesarParejaEncontradaGlobal(int idx1, int idx2)
    {
        if (contenedorMatriz == null) return;

        ControladorFicha[] todasLasFichas = contenedorMatriz.GetComponentsInChildren<ControladorFicha>(true);

        ControladorFicha f1 = null;
        ControladorFicha f2 = null;

        foreach (var ficha in todasLasFichas)
        {
            if (ficha.indiceEnTablero == idx1) f1 = ficha;
            if (ficha.indiceEnTablero == idx2) f2 = ficha;
        }

        Debug.Log($"🔍 Procesando pareja encontrada global -> IDs: [{idx1}] y [{idx2}]");

        if (f1 != null)
        {
            f1.estaEliminada = true;
            f1.MarcarComoEncontrada();
        }

        if (f2 != null)
        {
            f2.estaEliminada = true;
            f2.MarcarComoEncontrada();

            string palabraEsp = f2.traduccionFicha;
            string rutaAudioFicha = f2.rutaAudio;
            StartCoroutine(MostrarYQuitarPanelCo(palabraEsp, rutaAudioFicha));
        }

        parejasEncontradas++;
        if (parejasEncontradas >= totalParejasDelNivel)
        {
            Debug.Log("¡Nivel completado con éxito!");
            StartCoroutine(MostrarAvisoNivelCompletadoCo());
        }

        bloqueandoClics = false;
        ActualizarTextoTurno();
    }





}