// Fecha de creación: 25 de enero de 2026
using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
public class ControladorJuego : MonoBehaviour
{
    public static ControladorJuego Instance;


    [Header("Referencias de Puntaje")]
    public Transform ImgPuntosX; // Arrastra aquí el objeto del marcador X desde el Inspector de Unity
    public TextMeshProUGUI textoPuntajeX;
    public TextMeshProUGUI textoNombreX;
    public TextMeshProUGUI textoPuntajeY;

    public TextMeshProUGUI textoNombreY;
    private RawImage avatarMarcadorX;
    private RawImage avatarMarcadorY;
    public Transform ImgPuntosY; // Arrastra aquí el objeto del marcador Y desde el Inspector de Unity





    [Header("Datos de Sala Multijugador")]
    public string nombreSalaActual;
    public string idSalaActual;
    public string turnoActual;
    public int jugadorX;
    public int jugadorY;

    [Header("Datos del Usuario Actual")]
    public int id_player;
    public string nombre_jugador;
    public string avatar_url;
    public Dictionary<string, string> avataresPorJugador = new Dictionary<string, string>();

    public int puntosJugadorX;
    public int puntosJugadorY;
    private bool solicitudSiguienteNivelEnviada;

    [Header("Estado de Juego")]
    public bool esModoMultijugador = false;

    [Header("Configuración Socket")]
    public SocketIOUnity socket;

    [Header("Paneles de Interfaz")]
    public GameObject Panel_Login;
    public GameObject Panel_Lobby;
    public GameObject Panel_Juego;

    [Header("Referencias del Lobby Multijugador")]
    public Transform contenedorDeJugadores;
    public GameObject prefabItemJugador;

    [System.Serializable]
    public class DatosJugadorLobby
    {
        [JsonProperty("id_player")]
        public int id_player;

        [JsonProperty("username")]
        public string username;

        [JsonProperty("avatar_url")]
        public string avatar_url;
    }

    [System.Serializable]
    public class ItemNivel
    {
        public int id;
        public string audio;
        public string texto;
    }

    [System.Serializable]
    public class DatosPartida
    {
        public int nivel;
        public int total_parejas;
        public List<ItemNivelRespuesta> configuracion;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Application.runInBackground = true;
        ConfigurarMarcadores();
        ActualizarVisibilidadMarcadores();
        MostrarSoloLogin();
    }

    void Start()
    {
    }

    private void ConfigurarMarcadores()
    {
        if (ImgPuntosX == null)
            ImgPuntosX = GameObject.Find("ImgPuntosX")?.transform;
        if (ImgPuntosY == null)
            ImgPuntosY = GameObject.Find("ImgPuntosY")?.transform;
        avatarMarcadorX = CrearAvatarMarcador(ImgPuntosX, "AvatarX");
        avatarMarcadorY = CrearAvatarMarcador(ImgPuntosY, "AvatarY");
    }

    private void ActualizarVisibilidadMarcadores()
    {
        if (ImgPuntosY != null)
            ImgPuntosY.gameObject.SetActive(esModoMultijugador);
    }

    private RawImage CrearAvatarMarcador(Transform contenedor, string nombre)
    {
        if (contenedor == null) return null;
        Transform existente = contenedor.Find(nombre);
        if (existente == null)
        {
            Debug.LogWarning($"No existe la imagen {nombre} dentro de {contenedor.name}. Créala en Unity.");
            return null;
        }
        return existente.GetComponent<RawImage>();
    }

    private void CargarAvataresMarcador()
    {
        // Las referencias pueden no estar disponibles todavía si el objeto
        // persistente se creó antes de cargar la escena del tablero.
        if (avatarMarcadorX == null || (esModoMultijugador && avatarMarcadorY == null))
            ConfigurarMarcadores();

        string urlX = null;
        string urlY = null;
        avataresPorJugador.TryGetValue(textoNombreX != null ? textoNombreX.text : "", out urlX);
        avataresPorJugador.TryGetValue(textoNombreY != null ? textoNombreY.text : "", out urlY);
        if (string.IsNullOrEmpty(urlX) && !string.IsNullOrEmpty(nombre_jugador))
            avataresPorJugador.TryGetValue(nombre_jugador, out urlX);
        if (string.IsNullOrEmpty(urlX)) urlX = avatar_url;
        if (string.IsNullOrEmpty(urlY)) urlY = avatar_url;
        urlX = NormalizarUrlAvatar(urlX);
        urlY = NormalizarUrlAvatar(urlY);
        Debug.Log($"🖼️ Cargando avatares del marcador -> X: {urlX}, Y: {urlY}");
        if (avatarMarcadorX == null)
            Debug.LogWarning("No se encontró un RawImage llamado AvatarX dentro de ImgPuntosX.");
        if (!string.IsNullOrEmpty(urlX) && avatarMarcadorX != null) StartCoroutine(DescargarAvatarMarcador(urlX, avatarMarcadorX));
        if (!string.IsNullOrEmpty(urlY) && avatarMarcadorY != null) StartCoroutine(DescargarAvatarMarcador(urlY, avatarMarcadorY));
    }

    private string NormalizarUrlAvatar(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim();
        if (url.StartsWith("http://") || url.StartsWith("https://")) return url;
        if (url.StartsWith("/")) return "https://dokasoft.com" + url;
        return "https://dokasoft.com/dokasoft-coning/" + url.TrimStart('/');
    }

    private System.Collections.IEnumerator DescargarAvatarMarcador(string url, RawImage destino)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success && destino != null)
            {
                destino.texture = DownloadHandlerTexture.GetContent(request);
                Debug.Log($"✅ Avatar cargado correctamente en {destino.name}");
            }
            else if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"No se pudo cargar avatar del marcador desde {url}: {request.error}");
            else if (destino == null)
                Debug.LogWarning($"La URL del avatar respondió, pero el destino RawImage es nulo: {url}");
        }
    }

    public void ConfigurarSocket(string idPlayerRecibido)
    {
        if (string.IsNullOrEmpty(idPlayerRecibido) || idPlayerRecibido == "0")
        {
            Debug.LogWarning("⚠️ Se intentó configurar el socket con un ID inválido o vacío.");
            return;
        }

        if (socket != null) socket.Disconnect();

        var uri = new Uri(AppConfig.socketURL);

        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            EIO = EngineIO.V3,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            Query = new Dictionary<string, string>
            {
                { "id_player", idPlayerRecibido }
            }
        });

        SuscribirEventos();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("<color=green>¡Socket conectado correctamente al servidor!</color>");
        };

        socket.Connect();
    }

    void SuscribirEventos()
    {
        if (socket == null) return;

        socket.OnUnityThread("actualizar_lista_jugadores", (response) =>
        {
            try
            {
                string rawJson = response.ToString();
                List<DatosJugadorLobby> jugadores = null;

                try
                {
                    jugadores = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DatosJugadorLobby>>(rawJson);
                }
                catch
                {
                    var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
                    if (tokenArray.Count > 0)
                    {
                        jugadores = tokenArray[0].ToObject<List<DatosJugadorLobby>>();
                    }
                }

                if (jugadores != null)
                {
                    Debug.Log($"<color=cyan>Lista de jugadores recibida con éxito. Total:</color> {jugadores.Count}");
                    ActualizarListaVisual(jugadores);
                }
                else
                {
                    Debug.LogWarning("La lista de jugadores llegó nula.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'actualizar_lista_jugadores': " + e.Message);
            }
        });

        // 🌟 LISTENER GLOBAL: Cierra las fichas y cambia el turno cuando no hacen pareja
        socket.OnUnityThread("cerrar_fichas", (response) =>
     {
         Debug.Log("🚨 ¡EVENTO GLOBAL cerrar_fichas RECIBIDO!");
         try
         {
             string rawJson = response != null ? response.ToString() : "";
             int idx1 = -1;
             int idx2 = -1;
             string nuevoTurno = null;

             var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
             if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
             {
                 token = array[0];
             }

             if (token is Newtonsoft.Json.Linq.JObject obj)
             {
                 idx1 = obj["indice1"] != null ? (int)obj["indice1"] : -1;
                 idx2 = obj["indice2"] != null ? (int)obj["indice2"] : -1;

                 if (obj["nuevoTurno"] != null)
                 {
                     nuevoTurno = obj["nuevoTurno"].ToString();
                 }
             }

             if (!string.IsNullOrEmpty(nuevoTurno))
             {
                 ControladorJuego.Instance.turnoActual = nuevoTurno;
                 Debug.Log($"🔄 Turno actualizado tras fallo: [{nuevoTurno}]");

                 if (GestorTablero.Instance != null)
                 {
                     GestorTablero.Instance.ActualizarTextoTurno();
                 }
             }

             if (idx1 != -1 && idx2 != -1 && GestorTablero.Instance != null)
             {
                 GestorTablero.Instance.OcultarFichasLocales(idx1, idx2);
             }
         }
         catch (System.Exception e)
         {
             Debug.LogError("Error al procesar cerrar_fichas global: " + e.ToString());
         }
     });


        // 🌟 LISTENER GLOBAL: Revela una ficha cuando el rival la presiona
        socket.OnUnityThread("ficha_volteada_remota", (response) =>
        {
            try
            {
                string rawJson = response != null ? response.ToString() : "";
                var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
                if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0) token = array[0];

                if (token is Newtonsoft.Json.Linq.JObject obj && obj["indiceFicha"] != null)
                {
                    int indice = obj["indiceFicha"].ToObject<int>();
                    if (GestorTablero.Instance != null)
                    {
                        GestorTablero.Instance.RevelarFichaRemota(indice);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar ficha_volteada_remota global: " + e.ToString());
            }
        });

        // 🌟 LISTENER GLOBAL: Procesa el acierto de una pareja en la partida

        socket.OnUnityThread("pareja_encontrada", (response) =>
            {
                Debug.Log("🚨 ¡EVENTO GLOBAL pareja_encontrada RECIBIDO!");
                try
                {
                    string rawJson = response != null ? response.ToString() : "";
                    Debug.Log("JSON recibido: " + rawJson);

                    int idx1 = -1;
                    int idx2 = -1;
                    string nuevoTurno = null;
                    int puntosX = 0;
                    int puntosY = 0;
                    string jugadorQueAcerto = "";

                    // Intentamos parsearlo como un Token genérico (puede ser JObject o JArray)
                    var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
                    if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                    {
                        token = array[0]; // Si es un array, tomamos el primer elemento
                    }

                    if (token is Newtonsoft.Json.Linq.JObject obj)
                    {
                        idx1 = obj["indice1"] != null ? (int)obj["indice1"] : -1;
                        idx2 = obj["indice2"] != null ? (int)obj["indice2"] : -1;

                        // Capturamos el turno siguiente
                        if (obj["turnoActual"] != null)
                        {
                            nuevoTurno = obj["turnoActual"].ToString();
                        }
                        else if (obj["siguienteTurno"] != null)
                        {
                            nuevoTurno = obj["siguienteTurno"].ToString();
                        }

                        // 👇 1. Capturamos los puntos y quién acertó que manda el servidor
                        if (obj["puntosX"] != null) puntosX = (int)obj["puntosX"];
                        if (obj["puntosY"] != null) puntosY = (int)obj["puntosY"];
                        if (obj["jugadorQueAcerto"] != null) jugadorQueAcerto = obj["jugadorQueAcerto"].ToString();

                        puntosJugadorX = puntosX;
                        puntosJugadorY = puntosY;
                    }

                    if (!string.IsNullOrEmpty(nuevoTurno))
                    {
                        // Actualizamos el turno global en el controlador
                        ControladorJuego.Instance.turnoActual = nuevoTurno;
                        Debug.Log($"🔄 Turno actualizado por el servidor tras acierto: [{nuevoTurno}]");
                    }

                    // 👇 2. Actualizamos los textos en la UI de inmediato
                    if (ControladorJuego.Instance.textoPuntajeX != null)
                        ControladorJuego.Instance.textoPuntajeX.text = $"Pts {puntosX}";

                    if (ControladorJuego.Instance.textoPuntajeY != null)
                        ControladorJuego.Instance.textoPuntajeY.text = $"Pts {puntosY}";

                    // 👇 3. Disparamos la animación en el marcador del jugador que acertó
                    if (jugadorQueAcerto == "X" && ControladorJuego.Instance.ImgPuntosX != null)
                    {
                        ControladorJuego.Instance.StartCoroutine(EfectoAnimacionPunto(ControladorJuego.Instance.ImgPuntosX.transform));
                    }
                    else if (jugadorQueAcerto == "Y" && ControladorJuego.Instance.ImgPuntosY != null)
                    {
                        ControladorJuego.Instance.StartCoroutine(EfectoAnimacionPunto(ControladorJuego.Instance.ImgPuntosY.transform));
                    }

                    if (idx1 != -1 && idx2 != -1)
                    {
                        Debug.Log($"✅ Procesando pareja encontrada -> Indice1: {idx1}, Indice2: {idx2}");

                        if (GestorTablero.Instance != null)
                        {
                            GestorTablero.Instance.ProcesarParejaEncontradaGlobal(idx1, idx2);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error al procesar pareja_encontrada global: " + e.ToString());
                }
            });









        // La suscripción única de iniciar_partida se mantiene más abajo.
        // Este bloque antiguo queda desactivado para evitar procesar el evento dos veces.
        /* socket.OnUnityThread("iniciar_partida", (response) =>
        {
            try
            {
                Debug.Log("--- LLEGÓ EL EVENTO iniciar_partida ---");

                string rawJson = response != null ? response.ToString() : "RESPONSE ES NULL";
                Debug.Log("RAW JSON RECIBIDO: " + rawJson);

                DatosPartidaRespuesta respuesta = null;

                try
                {
                    // Socket.IO desde Node envuelve los objetos emitidos en un JArray
                    var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
                    if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                    {
                        token = array[0]; // Extraemos el objeto real del primer índice
                    }

                    respuesta = token.ToObject<DatosPartidaRespuesta>();
                }
                catch (System.Exception exJson)
                {
                    Debug.LogError("Error al parsear el paquete de inicio de partida: " + exJson.Message);
                }

                if (respuesta != null)
                {
                    Debug.Log($"¡Deserialización exitosa! NombreX: [{respuesta.nombreJugadorX}] | NombreY: [{respuesta.nombreJugadorY}]");

                    esModoMultijugador = true;
                    nombreSalaActual = respuesta.nombreSala;
                    turnoActual = respuesta.turnoActual;

                    if (textoNombreX != null && !string.IsNullOrEmpty(respuesta.nombreJugadorX))
                    {
                        textoNombreX.text = respuesta.nombreJugadorX;
                    }

                    if (textoNombreY != null && !string.IsNullOrEmpty(respuesta.nombreJugadorY))
                    {
                        textoNombreY.text = respuesta.nombreJugadorY;
                    }

                    if (Panel_Lobby != null) Panel_Lobby.SetActive(false);
                    if (Panel_Juego != null) Panel_Juego.SetActive(true);

                    if (GestorTablero.Instance != null)
                    {
                        GestorTablero.Instance.ConfigurarTablero(respuesta.fichas);

                        if (respuesta.fichas.Count > 0)
                        {
                            GestorTablero.Instance.CargarNuevaPalabra(respuesta.fichas[0].traduccion);
                        }

                        GestorTablero.Instance.ActualizarTextoTurno();
                    }
                }
                else
                {
                    Debug.LogError("¡El objeto 'respuesta' es NULL después de procesar el JSON!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error general en 'iniciar_partida': " + e.ToString());
            }
        }); */










        // 🌟 LISTENER GLOBAL: Inicia la partida tanto para modo solo como multijugador
        socket.OnUnityThread("iniciar_partida", (response) =>
        {
            try
            {
                Debug.Log("--- EVENTO INICIAR_PARTIDA RECIBIDO ---");

                string rawJson = response != null ? response.ToString() : "RESPONSE ES NULO";
                Debug.Log("JSON Crudo recibido: " + rawJson);

                DatosPartidaRespuesta respuesta = null;

                try
                {
                    respuesta = Newtonsoft.Json.JsonConvert.DeserializeObject<DatosPartidaRespuesta>(rawJson);
                    Debug.Log("Deserialización directa exitosa.");
                }
                catch (System.Exception exDirect)
                {
                    Debug.LogWarning("Falló deserialización directa, intentando con JArray... Motivo: " + exDirect.Message);
                    var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
                    if (tokenArray.Count > 0)
                    {
                        respuesta = tokenArray[0].ToObject<DatosPartidaRespuesta>();
                        Debug.Log("Deserialización con JArray exitosa.");
                    }
                }

                if (respuesta != null)
                {
                    Debug.Log($"Datos parseados -> Nivel: {respuesta.nivel} | Total Parejas: {respuesta.total_parejas}");
                    solicitudSiguienteNivelEnviada = false;
                    if ((respuesta.fichas == null || respuesta.fichas.Count == 0) && respuesta.configuracion != null)
                        respuesta.fichas = respuesta.configuracion;
                    Debug.Log($"Fichas recibidas para el nivel: {(respuesta.fichas != null ? respuesta.fichas.Count : 0)}");
                    if (respuesta.nivel > 0 && GestorTablero.Instance != null)
                        GestorTablero.Instance.nivelActual = respuesta.nivel;

                    // --- CONFIGURACIÓN DE MULTIJUGADOR O MODO SOLO ---
                    bool datosMultijugador = !string.IsNullOrEmpty(respuesta.nombreSala) ||
                                             respuesta.jugadorX > 0 || respuesta.jugadorY > 0 ||
                                             !string.IsNullOrEmpty(respuesta.nombreJugadorY);
                    if (datosMultijugador)
                    {
                        if (!string.IsNullOrEmpty(respuesta.nombreSala))
                            ControladorJuego.Instance.nombreSalaActual = respuesta.nombreSala;
                        ControladorJuego.Instance.turnoActual = respuesta.turnoActual;
                        ControladorJuego.Instance.jugadorX = respuesta.jugadorX;
                        ControladorJuego.Instance.jugadorY = respuesta.jugadorY;
                        ControladorJuego.Instance.esModoMultijugador = true;

                        Debug.Log($"[MULTIJUGADOR] Sala={respuesta.nombreSala}, X={respuesta.jugadorX}, Y={respuesta.jugadorY}");

                        // Si viene del multijugador, pintamos los nombres de los jugadores si existen
                        if (textoNombreX != null && !string.IsNullOrEmpty(respuesta.nombreJugadorX))
                            textoNombreX.text = respuesta.nombreJugadorX;

                        if (textoNombreY != null && !string.IsNullOrEmpty(respuesta.nombreJugadorY))
                            textoNombreY.text = respuesta.nombreJugadorY;
                        CargarAvataresMarcador();

                        Debug.Log($"👥 [MULTIJUGADOR ACTIVO] Sala: {respuesta.nombreSala} | Turno Inicial: {respuesta.turnoActual}");
                    }
                    else
                    {
                        ControladorJuego.Instance.esModoMultijugador = false;
                        Debug.Log("👤 [MODO SOLO ACTIVO]");

                        // En modo solo también debe mostrarse el avatar del usuario
                        // autenticado en el marcador X. El flujo anterior solo lo
                        // cargaba para partidas multijugador.
                        CargarAvataresMarcador();
                    }
                    ActualizarVisibilidadMarcadores();
                    // ------------------------------------------------

                    if (respuesta.fichas != null)
                    {
                        Debug.Log($"¡La lista 'fichas' NO is nula! Contiene {respuesta.fichas.Count} elementos.");
                    }
                    else
                    {
                        Debug.LogError("¡ATENCIÓN! La lista 'fichas' llegó como NULA dentro del JSON mapeado.");
                    }

                    // 1. Cambiamos de panel
                    if (Panel_Lobby != null) Panel_Lobby.SetActive(false);
                    if (Panel_Login != null) Panel_Login.SetActive(false);
                    if (Panel_Juego != null) Panel_Juego.SetActive(true);

                    // 2. Configuramos el tablero enviándole la lista de fichas a través del GestorTablero
                    if (GestorTablero.Instance != null)
                    {

                        GestorTablero.Instance.ConfigurarTablero(respuesta.fichas);

                        // 3. Cargamos la primera traducción disponible al iniciar el nivel
                        if (respuesta.fichas != null && respuesta.fichas.Count > 0)
                        {
                            GestorTablero.Instance.CargarNuevaPalabra(respuesta.fichas[0].traduccion);
                        }

                        GestorTablero.Instance.ActualizarTextoTurno();
                    }
                    else
                    {
                        Debug.LogError("El GestorTablero.Instance está nulo en ControladorJuego.");
                    }
                }
                else
                {
                    Debug.LogError("El objeto 'respuesta' quedó nulo después de intentar deserializar.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'iniciar_partida': " + e.ToString());
            }
        });








    }


    // 📌 Coroutine para hacer la animación de zoom en el marcador del jugador que sumó puntos
    private System.Collections.IEnumerator EfectoAnimacionPunto(Transform objetoPuntuacion)
    {
        Vector3 escalaOriginal = objetoPuntuacion.localScale;
        Vector3 escalaGrande = escalaOriginal * 1.3f; // Tamaño máximo al hacer zoom

        // ⏱️ Aumenta este valor para que el crecimiento sea más lento y notable (ej. 0.3 segundos)
        float tiempoCrecimiento = 0.3f;
        float elaps = 0;
        while (elaps < tiempoCrecimiento)
        {
            objetoPuntuacion.localScale = Vector3.Lerp(escalaOriginal, escalaGrande, elaps / tiempoCrecimiento);
            elaps += Time.deltaTime;
            yield return null;
        }
        objetoPuntuacion.localScale = escalaGrande;

        // ⏱️ Opcional: un pequeño respiro en tamaño grande antes de encoger (ej. 0.2 segundos quieto)
        yield return new WaitForSeconds(0.2f);

        // ⏱️ Aumenta este valor para que regrese a la normalidad con más calma (ej. 0.3 segundos)
        float tiempoRegreso = 0.3f;
        elaps = 0;
        while (elaps < tiempoRegreso)
        {
            objetoPuntuacion.localScale = Vector3.Lerp(escalaGrande, escalaOriginal, elaps / tiempoRegreso);
            elaps += Time.deltaTime;
            yield return null;
        }
        objetoPuntuacion.localScale = escalaOriginal;
    }
    void MostrarSoloLogin()
    {
        if (Panel_Login != null)
        {
            Panel_Login.SetActive(true);
        }

        if (Panel_Lobby != null)
        {
            Panel_Lobby.SetActive(false);
        }
    }

    public void ActualizarListaVisual(List<DatosJugadorLobby> jugadores)
    {
        foreach (Transform child in contenedorDeJugadores)
        {
            Destroy(child.gameObject);
        }

        foreach (DatosJugadorLobby jugador in jugadores)
        {
            // El servidor incluye al usuario actual en la lista. No lo mostramos
            // para evitar que pueda invitarse a sí mismo ni confundir su avatar.
            if (jugador.id_player == id_player ||
                (!string.IsNullOrEmpty(nombre_jugador) &&
                 string.Equals(jugador.username, nombre_jugador, System.StringComparison.OrdinalIgnoreCase)))
                continue;

            avataresPorJugador[jugador.username] = jugador.avatar_url;
            GameObject nuevoItem = Instantiate(prefabItemJugador, contenedorDeJugadores);
            jugadorPrefab item = nuevoItem.GetComponent<jugadorPrefab>();
            if (item != null)
            {
                item.Inicializar(jugador.id_player.ToString(), jugador.username, jugador.avatar_url);
            }
        }
    }

    public void InvitarJugador(string idReceptorDeseado)
    {
        if (socket == null || !socket.Connected) return;

        var datosInvitacion = new Dictionary<string, object>
        {
            { "idEmisor", id_player.ToString() },
            { "idReceptor", idReceptorDeseado },
            { "nombreEmisor", nombre_jugador }
        };

        socket.Emit("enviar_invitacion", datosInvitacion);
        Debug.Log($"Enviando invitación de juego al usuario ID: {idReceptorDeseado}");
    }

    public void SolicitarSiguienteNivel(string nombreSala, int nivelActual)
    {
        if (socket == null || string.IsNullOrEmpty(nombreSala) || solicitudSiguienteNivelEnviada) return;

        solicitudSiguienteNivelEnviada = true;

        int idReceptor = jugadorX == id_player ? jugadorY : jugadorX;
        var datosAEnviar = new
        {
            nombreSala = nombreSala,
            nivelActual = nivelActual,
            idEmisor = id_player,
            idReceptor = idReceptor
        };

        // Emitimos el evento al servidor Node.js
        socket.Emit("solicitar_siguiente_nivel", datosAEnviar);
        Debug.Log($"Emitiendo 'solicitar_siguiente_nivel' para la sala: {nombreSala}, partiendo del nivel: {nivelActual}");
    }



}

