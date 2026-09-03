using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GestorTablero : MonoBehaviour
{
    [Header("UI de Turnos")]
    public TextMeshProUGUI textoTurnoUI;

    [Header("Panel de Traducción")]
    public GameObject panelTraduccionObjeto;
    public TextMeshProUGUI textoTraduccionPanel;
    public float tiempoVisiblePanel = 2f;

    public static GestorTablero Instance;
    public int nivelActual = 1;
    public TextMeshProUGUI textoNivelTitulo;

    [Header("UI Traducción")]
    public TextMeshProUGUI textoTraduccionUI;
    private string traduccionActual = "";
    private bool estaRevelado = false;

    [Header("Referencias de Victoria / Siguiente Nivel")]
    [FormerlySerializedAs("panelNivelCompletado")]
    public GameObject panelVictoria;
    public GameObject panelVictoriaIndividual;
    private int parejasEncontradas = 0;
    private int totalParejasDelNivel = 0;

    [Header("Referencias de Paneles para Navegación")]
    public GameObject panelJuego;
    public GameObject panelLobby;

    [Header("Referencias de UI")]
    public Transform contenedorMatriz;
    public GameObject prefabCuadro;

    private bool bloqueandoClics = false;
    private ControladorFicha primeraFichaLocal;

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
        if (listaFichas == null)
        {
            Debug.LogError("iniciar_partida recibió fichas nulas.");
            return;
        }
        if (contenedorMatriz == null)
            contenedorMatriz = GameObject.Find("ContenedorMatriz")?.transform;
        if (contenedorMatriz == null || prefabCuadro == null)
        {
            Debug.LogError($"No se puede crear el tablero. Contenedor={(contenedorMatriz != null)}, Prefab={(prefabCuadro != null)}");
            return;
        }
        Debug.Log($"Configurando tablero con {listaFichas.Count} fichas.");

        foreach (Transform child in contenedorMatriz)
        {
            Destroy(child.gameObject);
        }

        bloqueandoClics = false;
        primeraFichaLocal = null;
        parejasEncontradas = 0;
        totalParejasDelNivel = listaFichas.Count / 2;

        if (panelVictoria != null)
            panelVictoria.SetActive(false);
        if (panelVictoriaIndividual != null)
            panelVictoriaIndividual.SetActive(false);

        for (int i = 0; i < listaFichas.Count; i++)
        {
            var ficha = listaFichas[i];
            if (ficha.id == 0 && ficha.fichaid != 0) ficha.id = ficha.fichaid;
            if (string.IsNullOrEmpty(ficha.audio)) ficha.audio = ficha.ruta_audio;
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

        if (!esMultijugadorReal)
        {
            ProcesarSeleccionLocal(ficha);
            return;
        }

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

    private void ProcesarSeleccionLocal(ControladorFicha ficha)
    {
        if (primeraFichaLocal == null)
        {
            primeraFichaLocal = ficha;
            return;
        }

        ControladorFicha primeraFicha = primeraFichaLocal;
        ControladorFicha segundaFicha = ficha;
        primeraFichaLocal = null;
        bloqueandoClics = true;

        if (primeraFicha == segundaFicha)
        {
            bloqueandoClics = false;
            return;
        }

        if (primeraFicha.idFicha == segundaFicha.idFicha || primeraFicha.textoPalabra == segundaFicha.textoPalabra)
        {
            // En modo solo usamos las referencias reales de las fichas. Esto evita
            // depender de indiceEnTablero cuando el servidor no lo envía o lo repite.
            ProcesarParejaEncontradaLocal(primeraFicha, segundaFicha);
        }
        else
        {
            StartCoroutine(OcultarParejaFallidaLocal(primeraFicha, segundaFicha));
        }
    }

    private System.Collections.IEnumerator OcultarParejaFallidaLocal(ControladorFicha ficha1, ControladorFicha ficha2)
    {
        yield return new WaitForSeconds(0.8f);
        if (ficha1 != null) ficha1.OcultarFicha();
        if (ficha2 != null) ficha2.OcultarFicha();
        bloqueandoClics = false;
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
        if (panelVictoria != null) panelVictoria.SetActive(true);
    }

    private void ProcesarParejaEncontradaLocal(ControladorFicha ficha1, ControladorFicha ficha2)
    {
        // Preparar el marcador antes de abrir el panel de victoria (la pareja
        // que se está cerrando también debe aparecer en el puntaje final).
        if (ControladorJuego.Instance != null && !ControladorJuego.Instance.esModoMultijugador)
        {
            ControladorJuego.Instance.puntosJugadorX = parejasEncontradas + 1;
            if (ControladorJuego.Instance.textoPuntajeX != null)
                ControladorJuego.Instance.textoPuntajeX.text = $"Pts {parejasEncontradas + 1}";
        }

        ProcesarParejaEncontrada(ficha1, ficha2);

        // En modo solo no existe el evento de puntuación del servidor; actualizamos
        // el marcador local después de cerrar la pareja.
        if (ControladorJuego.Instance != null && !ControladorJuego.Instance.esModoMultijugador)
        {
            ControladorJuego.Instance.puntosJugadorX = parejasEncontradas;
            if (ControladorJuego.Instance.textoPuntajeX != null)
                ControladorJuego.Instance.textoPuntajeX.text = $"Pts {parejasEncontradas}";
        }
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
        Debug.Log($"🔍 Buscando archivo : [{rutaAudioRelativa}]");
        if (string.IsNullOrEmpty(rutaAudioRelativa)) return;

        // Limpiamos la extensión y las plecas iniciales
        string rutaLimpia = rutaAudioRelativa.Replace(".mp3", "").Replace(".wav", "").TrimStart('/');

        // Si la ruta viene con "sonidos/", se lo quitamos porque la carpeta física raíz dentro de Resources es "niveles"
        if (rutaLimpia.StartsWith("sonidos/"))
        {
            rutaLimpia = rutaLimpia.Substring("sonidos/".Length);
        }

        Debug.Log($"🔍 Buscando archivo físicamente en Resources con ruta final: [{rutaLimpia}]");
        StartCoroutine(DescargarYReproducirAudio(rutaLimpia));
    }

    private System.Collections.IEnumerator DescargarYReproducirAudio(string rutaAudio)
    {
        // Quitamos la extensión .mp3 o .wav porque Resources.Load no la requiere
        string rutaSinExtension = rutaAudio.Replace(".mp3", "").Replace(".wav", "");

        // Cargamos el AudioClip directamente desde los recursos locales de Unity
        ResourceRequest request = Resources.LoadAsync<AudioClip>(rutaSinExtension);
        yield return request;

        AudioClip clip = request.asset as AudioClip;

        if (clip != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró el audio en los recursos locales: " + rutaSinExtension);
        }
    }

    public void BotonSiguienteNivel_Click()
    {
        ControladorVictoria victoria = FindFirstObjectByType<ControladorVictoria>();
        if (victoria != null) victoria.DetenerTemporizador();
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelVictoriaIndividual != null) panelVictoriaIndividual.SetActive(false);
        nivelActual++;
        if (textoNivelTitulo != null) textoNivelTitulo.text = "Nivel " + nivelActual;
        if (panelJuego != null) panelJuego.SetActive(true);

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.esModoMultijugador)
        {
            // En multijugador el servidor debe avisar a ambos jugadores.
            ControladorJuego.Instance.SolicitarSiguienteNivel(
                ControladorJuego.Instance.nombreSalaActual,
                nivelActual - 1);
        }
        else
        {
            FindFirstObjectByType<ControladorModoSolo>().SolicitarModoIndividual();
        }
    }

    public void BotonMenuPrincipal_Click()
    {
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelVictoriaIndividual != null) panelVictoriaIndividual.SetActive(false);
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

        // El flujo multijugador localiza las fichas por su índice lógico.
        ProcesarParejaEncontrada(f1, f2);
    }

    private void ProcesarParejaEncontrada(ControladorFicha f1, ControladorFicha f2)
    {
        if (f1 == null || f2 == null || f1 == f2)
        {
            Debug.LogWarning("No se pudo cerrar la pareja: no se encontraron dos fichas distintas.");
            bloqueandoClics = false;
            return;
        }

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
            int nivelActualPartida = nivelActual;
            string nombreSalaActual = (ControladorJuego.Instance != null) ? ControladorJuego.Instance.nombreSalaActual : "";

            bool esModoMultijugador = ControladorJuego.Instance != null && ControladorJuego.Instance.esModoMultijugador;
            GameObject panelActivo = esModoMultijugador ? panelVictoria : panelVictoriaIndividual;

            // Compatibilidad con escenas donde las referencias no se asignaron en el Inspector.
            if (panelActivo == null)
                panelActivo = BuscarPanelPorNombre(esModoMultijugador ? "PanelVictoria" : "PanelVictoriaIndividual");
            if (panelActivo == null && esModoMultijugador)
                panelActivo = BuscarPanelPorNombre("PanelNivelCompletado");

            if (esModoMultijugador)
                panelVictoria = panelActivo;
            else
                panelVictoriaIndividual = panelActivo;

            Debug.Log($"¡Nivel completado con éxito! Nivel: {nivelActualPartida} | Sala: {nombreSalaActual}");

            if (panelActivo != null)
            {
                if (panelVictoria != null && panelVictoria != panelActivo) panelVictoria.SetActive(false);
                if (panelVictoriaIndividual != null && panelVictoriaIndividual != panelActivo) panelVictoriaIndividual.SetActive(false);
                panelActivo.SetActive(true);

                ControladorVictoria controladorVictoria = panelActivo.GetComponentInChildren<ControladorVictoria>(true);
                if (controladorVictoria == null)
                    controladorVictoria = panelActivo.AddComponent<ControladorVictoria>();

                if (controladorVictoria != null)
                {
                    // Llamamos al método con los 2 argumentos originales que sí existen
                    string nombreX = ControladorJuego.Instance != null && ControladorJuego.Instance.textoNombreX != null ? ControladorJuego.Instance.textoNombreX.text : "Jugador X";
                    string nombreY = ControladorJuego.Instance != null && ControladorJuego.Instance.textoNombreY != null ? ControladorJuego.Instance.textoNombreY.text : "Jugador Y";
                    int puntosX = ControladorJuego.Instance != null ? ControladorJuego.Instance.puntosJugadorX : parejasEncontradas;
                    int puntosY = ControladorJuego.Instance != null ? ControladorJuego.Instance.puntosJugadorY : 0;
                    if (ControladorJuego.Instance == null || !ControladorJuego.Instance.esModoMultijugador)
                        puntosX = parejasEncontradas;
                    controladorVictoria.ActivarPantallaVictoria(nivelActualPartida, nombreSalaActual, nombreX, puntosX, nombreY, puntosY);
                }
                else
                {
                    Debug.LogError("⚠️ No se encontró el componente ControladorVictoria en el panel de nivel completado.");
                }
            }
            else
            {
                Debug.LogError(esModoMultijugador
                    ? "No se encontró PanelVictoria. Asígnalo en GestorTablero."
                    : "No se encontró PanelVictoriaIndividual. Asígnalo en GestorTablero.");
            }
        }

        bloqueandoClics = false;
        ActualizarTextoTurno();
    }

    private GameObject BuscarPanelPorNombre(string nombre)
    {
        // GameObject.Find no devuelve objetos inactivos; el panel de victoria
        // normalmente empieza desactivado, por eso buscamos también en la escena
        // cargada mediante Resources.FindObjectsOfTypeAll.
        foreach (GameObject objeto in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (objeto.name == nombre && objeto.scene.IsValid())
                return objeto;
        }
        return null;
    }





}
