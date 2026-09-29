// Fecha de creación: 25 de enero de 2026
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

// Solo cargamos la librería de C# si NO estamos en WebGL o si estamos en el Editor
#if !(UNITY_WEBGL && !UNITY_EDITOR)
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
#endif

public class ControladorJuego : MonoBehaviour
{

    public bool estaConectadoAlSocket = false;
    public static ControladorJuego Instance;

    [Header("panel ensayo")]
    public GameObject Panel_Connection;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void JS_ConectarSocket(string url, string path, string idPlayer);
    [DllImport("__Internal")]
    private static extern void JS_EmitirEvento(string evento, string datosJson);
    
    [HideInInspector] public SocketIOUnity socket = null; 
#else
    public SocketIOUnity socket;
#endif

    [Header("Referencias de Puntaje")]
    public Transform ImgPuntosX;
    public TextMeshProUGUI textoPuntajeX;
    public TextMeshProUGUI textoNombreX;
    public TextMeshProUGUI textoPuntajeY;
    public TextMeshProUGUI textoNombreY;
    private RawImage avatarMarcadorX;
    private RawImage avatarMarcadorY;
    public Transform ImgPuntosY;

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
    public bool modoSoloSolicitado = false;
    public bool modoMaquina = false;
    public int dificultadMaquina = 1;

    [Header("Paneles de Interfaz")]
    public GameObject Panel_Login;
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
    public class MensajeWebGL
    {
        public string evento;
        public Newtonsoft.Json.Linq.JToken datos;
    }

    void Awake()
    {
        // Forzamos la asignación estática de inmediato al despertar
        Instance = this;

        if (gameObject.transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
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
            ImgPuntosY.gameObject.SetActive(esModoMultijugador || modoMaquina);
    }

    private RawImage CrearAvatarMarcador(Transform contenedor, string nombre)
    {
        if (contenedor == null) return null;
        Transform existente = contenedor.Find(nombre);
        if (existente == null)
        {
            foreach (Transform hijo in contenedor.GetComponentsInChildren<Transform>(true))
            {
                if (hijo.name == nombre)
                {
                    existente = hijo;
                    break;
                }
            }
        }
        if (existente == null) return null;
        RawImage imagen = existente.GetComponent<RawImage>();
        return imagen;
    }

    public Texture ObtenerAvatarActualDelMarcador()
    {
        if (avatarMarcadorX == null)
            ConfigurarMarcadores();

        return avatarMarcadorX != null ? avatarMarcadorX.texture : null;
    }

    private void CargarAvataresMarcador()
    {
        if (avatarMarcadorX == null || ((esModoMultijugador || modoMaquina) && avatarMarcadorY == null))
            ConfigurarMarcadores();

        string urlX = null;
        string urlY = null;

        // Intentamos obtener la URL del diccionario usando el nombre del texto o el nombre global del usuario
        string nombreXBusqueda = textoNombreX != null ? textoNombreX.text : "";
        if (!string.IsNullOrEmpty(nombreXBusqueda))
        {
            avataresPorJugador.TryGetValue(nombreXBusqueda, out urlX);
        }

        if (string.IsNullOrEmpty(urlX) && !string.IsNullOrEmpty(nombre_jugador))
        {
            avataresPorJugador.TryGetValue(nombre_jugador, out urlX);
        }

        // RESPALDO DE EMERGENCIA: Si sigue vacía, forzamos el avatar del usuario actual (ej. nando.png)
        if (string.IsNullOrEmpty(urlX))
        {
            urlX = !string.IsNullOrEmpty(avatar_url) ? avatar_url : "nando.png";
        }

        if (modoMaquina)
        {
            urlY = null;
            if (textoNombreY != null) textoNombreY.text = "Máquina";
            if (textoPuntajeY != null) textoPuntajeY.text = "Pts 0";
            if (avatarMarcadorY != null) avatarMarcadorY.texture = null;
        }
        else
        {
            string nombreYBusqueda = textoNombreY != null ? textoNombreY.text : "";
            if (!string.IsNullOrEmpty(nombreYBusqueda))
            {
                avataresPorJugador.TryGetValue(nombreYBusqueda, out urlY);
            }
            if (string.IsNullOrEmpty(urlY))
            {
                urlY = avatar_url;
            }
        }

        Debug.Log($"🔍 [Depuración Avatar] URL X antes de normalizar: '{urlX}'");
        Debug.Log($"🔍 [Depuración Avatar] URL Y antes de normalizar: '{urlY}'");

        urlX = NormalizarUrlAvatar(urlX);
        urlY = NormalizarUrlAvatar(urlY);

        if (!string.IsNullOrEmpty(urlX) && avatarMarcadorX != null)
            StartCoroutine(DescargarAvatarMarcador(urlX, avatarMarcadorX));

        if (!string.IsNullOrEmpty(urlY) && avatarMarcadorY != null)
            StartCoroutine(DescargarAvatarMarcador(urlY, avatarMarcadorY));
    }

    private string NormalizarUrlAvatar(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim();

        // Si viene con la URL absoluta, extraemos el nombre del archivo para apuntar al proxy local
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            string fileName = System.IO.Path.GetFileName(url);
            return "https://dokasoft.com/imagenes/avatar/" + fileName;
        }

        if (url.StartsWith("/")) return "https://dokasoft.com" + url;

        return "https://dokasoft.com/imagenes/avatar/" + url.TrimStart('/');
    }

    private System.Collections.IEnumerator DescargarAvatarMarcador(string url, RawImage destino)
    {
        Debug.Log($"📥 [ControladorJuego] Iniciando descarga de textura desde: {url}");

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success && destino != null)
            {
                Texture2D textura = DownloadHandlerTexture.GetContent(request);
                destino.texture = textura;
                Debug.Log("🎉 [ControladorJuego] ¡Avatar descargado y asignado al marcador con éxito!");
            }
            else
            {
                Debug.LogError($"❌ [ControladorJuego] Error descargando avatar desde {url}. Motivo: {request.error}");
            }
        }
    }


    public void ConfigurarSocket(string idPlayerRecibido)
    {
        UnityEngine.Debug.Log("[Gestor] Intentando configurar socket con ID: " + idPlayerRecibido);

        if (string.IsNullOrEmpty(idPlayerRecibido) || idPlayerRecibido == "0") return;

        // Reseteamos el estado al intentar una nueva conexión
        estaConectadoAlSocket = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    string urlWebGL = "https://dokasoft.com";
    string pathWebGL = "/coning/socket.io";
    JS_ConectarSocket(urlWebGL, pathWebGL, idPlayerRecibido);
    // Nota: Para WebGL, si manejas la conexión por JS, puedes marcar estaConectadoAlSocket = true 
    // cuando recibas el evento de conexión desde tu script de reenvío en JS.
#else
        if (socket != null)
        {
            try { socket.Disconnect(); socket.Dispose(); } catch { }
            socket = null;
        }

        var uri = new Uri("http://dokasoft.com:3010");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            EIO = EngineIO.V3,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            Query = new Dictionary<string, string>
        {
            { "id_player", idPlayerRecibido }
        }
        });

        UnityEngine.Debug.Log("[Socket C#] Configurando cliente nativo para APK/Editor...");

        socket.OnConnected += (sender, e) =>
        {
            UnityEngine.Debug.Log("[Socket C#] ¡Conectado al servidor con éxito!");

            // Marcamos la bandera en true al establecerse la conexión real
            estaConectadoAlSocket = true;

            socket.OnUnityThread("actualizar_lista_jugadores", (response) =>
            {
                UnityEngine.Debug.Log("[Socket C#] Evento recibido: actualizar_lista_jugadores");
                string jsonString = response.GetValue().ToString();
                ProcesarActualizarListaJugadores(jsonString);
            });

            socket.OnUnityThread("iniciar_partida", (response) =>
            {
                UnityEngine.Debug.Log("[Socket C#] Evento recibido: iniciar_partida -> " + response);
                ProcesarIniciarPartida(response != null ? response.ToString() : "");
            });
        };

        socket.Connect();
#endif
    }



    //     public void ConfigurarSocket(string idPlayerRecibido)
    //     {

    //         UnityEngine.Debug.Log("[Gestor] Intentando configurar socket con ID: " + idPlayerRecibido);

    //         if (string.IsNullOrEmpty(idPlayerRecibido) || idPlayerRecibido == "0") return;

    // #if UNITY_WEBGL && !UNITY_EDITOR
    //     string urlWebGL = "https://dokasoft.com";
    //     string pathWebGL = "/coning/socket.io";
    //     JS_ConectarSocket(urlWebGL, pathWebGL, idPlayerRecibido);
    // #else
    //         if (socket != null)
    //         {
    //             try { socket.Disconnect(); socket.Dispose(); } catch { }
    //             socket = null;
    //         }

    //         var uri = new Uri("http://dokasoft.com:3010");
    //         socket = new SocketIOUnity(uri, new SocketIOOptions
    //         {
    //             EIO = EngineIO.V3,
    //             Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
    //             Query = new Dictionary<string, string>
    //         {
    //             { "id_player", idPlayerRecibido }
    //         }
    //         });

    //         // Forzamos un log aquí mismo para confirmar que entró al código de C#
    //         UnityEngine.Debug.Log("[Socket C#] Configurando cliente nativo para APK/Editor...");

    //         socket.OnConnected += (sender, e) =>
    //         {
    //             UnityEngine.Debug.Log("[Socket C#] ¡Conectado al servidor con éxito!");

    //             // Llamamos directamente a las escuchas aquí adentro


    //             socket.OnUnityThread("actualizar_lista_jugadores", (response) =>
    //             {
    //                 UnityEngine.Debug.Log("[Socket C#] Evento recibido: actualizar_lista_jugadores");

    //                 // Obtenemos el texto JSON puro directamente del payload del socket
    //                 string jsonString = response.GetValue().ToString();

    //                 // Se lo pasamos a tu método central para que pinte los jugadores
    //                 ProcesarActualizarListaJugadores(jsonString);
    //             });




    //             socket.OnUnityThread("iniciar_partida", (response) =>
    //             {
    //                 UnityEngine.Debug.Log("[Socket C#] Evento recibido: iniciar_partida -> " + response);
    //                 ProcesarIniciarPartida(response != null ? response.ToString() : "");
    //             });
    //         };

    //         socket.Connect();
    // #endif
    //     }



    void SuscribirEventos()
    {
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        if (socket == null) return;
        Debug.Log("[APK] Registrando escuchas de eventos en C#...");
        socket.OnUnityThread("actualizar_lista_jugadores", (response) =>
        {
            Debug.Log("[APK] ¡Evento recibido! actualizar_lista_jugadores: " + response);
            ProcesarActualizarListaJugadores(response != null ? response.ToString() : "");
        });

        socket.OnUnityThread("iniciar_partida", (response) =>
        {
            ProcesarIniciarPartida(response != null ? response.ToString() : "");
        });
#endif
    }


    public void ProcesarActualizarListaJugadores(string rawJson)
    {
        try
        {
            List<DatosJugadorLobby> jugadores = null;

            if (string.IsNullOrEmpty(rawJson)) return;

            try
            {
                // Intento 1: Como lista directa
                jugadores = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DatosJugadorLobby>>(rawJson);
            }
            catch
            {
                try
                {
                    // Intento 2: Como un JToken genérico para evaluar si es array u objeto
                    var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
                    if (token.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                    {
                        jugadores = token.ToObject<List<DatosJugadorLobby>>();
                    }
                    else if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                    {
                        var unJugador = token.ToObject<DatosJugadorLobby>();
                        if (unJugador != null)
                        {
                            jugadores = new List<DatosJugadorLobby> { unJugador };
                        }
                    }
                }
                catch (System.Exception exInner)
                {
                    Debug.LogWarning("No se pudo parsear como estructura estándar: " + exInner.Message);
                }
            }

            if (jugadores != null && jugadores.Count > 0)
            {
                ActualizarListaVisual(jugadores);
            }
            else
            {
                Debug.LogWarning("La lista de jugadores llegó vacía o nula.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al procesar 'actualizar_lista_jugadores': " + e.Message);
        }
    }

    // Métodos centrales de procesamiento compartidos (Editor y WebGL)
    // // // public void ProcesarActualizarListaJugadores(string rawJson)
    // // // {
    // // //     try
    // // //     {
    // // //         List<DatosJugadorLobby> jugadores = null;

    // // //         try
    // // //         {
    // // //             jugadores = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DatosJugadorLobby>>(rawJson);
    // // //         }
    // // //         catch
    // // //         {
    // // //             var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
    // // //             if (tokenArray.Count > 0)
    // // //             {
    // // //                 jugadores = tokenArray[0].ToObject<List<DatosJugadorLobby>>();
    // // //             }
    // // //         }

    // // //         if (jugadores != null)
    // // //         {
    // // //             ActualizarListaVisual(jugadores);
    // // //         }
    // // //     }
    // // //     catch (System.Exception e)
    // // //     {
    // // //         Debug.LogError("Error al procesar 'actualizar_lista_jugadores': " + e.Message);
    // // //     }
    // // // }

    public void ProcesarIniciarPartida(string rawJson)
    {
        try
        {
            if (!string.IsNullOrEmpty(rawJson))
            {
                rawJson = rawJson.Trim();
                if (rawJson.StartsWith("\"") && rawJson.EndsWith("\""))
                {
                    try { rawJson = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(rawJson); } catch { }
                }
            }

            Debug.Log("🔍 RAW JSON RECIBIDO: " + rawJson);

            DatosPartidaRespuesta respuesta = null;

            try
            {
                respuesta = Newtonsoft.Json.JsonConvert.DeserializeObject<DatosPartidaRespuesta>(rawJson);
            }
            catch
            {
                var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
                if (tokenArray.Count > 0)
                {
                    respuesta = tokenArray[0].ToObject<DatosPartidaRespuesta>();
                }
            }

            if (respuesta != null)
            {
                solicitudSiguienteNivelEnviada = false;
                if ((respuesta.fichas == null || respuesta.fichas.Count == 0) && respuesta.configuracion != null)
                    respuesta.fichas = respuesta.configuracion;

                if (respuesta.nivel > 0 && GestorTablero.Instance != null)
                    GestorTablero.Instance.nivelActual = respuesta.nivel;

                bool datosMultijugador = !ControladorJuego.Instance.modoSoloSolicitado &&
                                         (!string.IsNullOrEmpty(respuesta.nombreSala) ||
                                          respuesta.jugadorX > 0 || respuesta.jugadorY > 0 ||
                                          !string.IsNullOrEmpty(respuesta.nombreJugadorY));

                if (datosMultijugador)
                {
                    ControladorJuego.Instance.modoSoloSolicitado = false;
                    if (!string.IsNullOrEmpty(respuesta.nombreSala))
                        ControladorJuego.Instance.nombreSalaActual = respuesta.nombreSala;
                    ControladorJuego.Instance.turnoActual = respuesta.turnoActual;
                    ControladorJuego.Instance.jugadorX = respuesta.jugadorX;
                    ControladorJuego.Instance.jugadorY = respuesta.jugadorY;
                    ControladorJuego.Instance.esModoMultijugador = true;
                    ControladorJuego.Instance.modoMaquina = false;

                    if (textoNombreX != null && !string.IsNullOrEmpty(respuesta.nombreJugadorX))
                        textoNombreX.text = respuesta.nombreJugadorX;

                    if (textoNombreY != null && !string.IsNullOrEmpty(respuesta.nombreJugadorY))
                        textoNombreY.text = respuesta.nombreJugadorY;

                    CargarAvataresMarcador();
                }
                else
                {
                    ControladorJuego.Instance.esModoMultijugador = false;
                    ControladorJuego.Instance.modoSoloSolicitado = false;
                    CargarAvataresMarcador();

                    int nivelElegido = PlayerPrefs.GetInt("NivelSeleccionado", respuesta.nivel > 0 ? respuesta.nivel : 1);
                    respuesta.nivel = nivelElegido;
                    if (GestorTablero.Instance != null)
                    {
                        GestorTablero.Instance.nivelActual = nivelElegido;
                        if (GestorTablero.Instance.textoNivelTitulo != null)
                            GestorTablero.Instance.textoNivelTitulo.text = "Nivel " + nivelElegido;
                    }
                }
                ActualizarVisibilidadMarcadores();


                if (Panel_Login != null) Panel_Login.SetActive(false);
                if (Panel_Juego != null) Panel_Juego.SetActive(true);

                if (GestorTablero.Instance != null)
                {
                    GestorTablero.Instance.ConfigurarTablero(respuesta.fichas);

                    if (respuesta.fichas != null && respuesta.fichas.Count > 0)
                    {
                        GestorTablero.Instance.CargarNuevaPalabra(respuesta.fichas[0].traduccion);
                    }

                    GestorTablero.Instance.ActualizarTextoTurno();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al procesar 'iniciar_partida' en WebGL/APK: " + e.ToString());
        }
    }

    void MostrarSoloLogin()
    {
        if (Panel_Login != null) Panel_Login.SetActive(true);

    }

    // // // public void ActualizarListaVisual(List<DatosJugadorLobby> jugadores)
    // // // {
    // // //     foreach (Transform child in contenedorDeJugadores)
    // // //     {
    // // //         Destroy(child.gameObject);
    // // //     }

    // // //     foreach (DatosJugadorLobby jugador in jugadores)
    // // //     {
    // // //         if (jugador.id_player == id_player ||
    // // //             (!string.IsNullOrEmpty(nombre_jugador) &&
    // // //              string.Equals(jugador.username, nombre_jugador, System.StringComparison.OrdinalIgnoreCase)))
    // // //         {
    // // //             if (!string.IsNullOrEmpty(jugador.username) && !string.IsNullOrEmpty(jugador.avatar_url))
    // // //                 avataresPorJugador[jugador.username] = jugador.avatar_url;
    // // //             continue;
    // // //         }

    // // //         avataresPorJugador[jugador.username] = jugador.avatar_url;
    // // //         GameObject nuevoItem = Instantiate(prefabItemJugador, contenedorDeJugadores);
    // // //         jugadorPrefab item = nuevoItem.GetComponent<jugadorPrefab>();
    // // //         if (item != null)
    // // //         {
    // // //             item.Inicializar(jugador.id_player.ToString(), jugador.username, jugador.avatar_url);
    // // //         }
    // // //     }


    // // //     // <--- Forzamos el redibujo aquí afuera del foreach --->
    // // //     Canvas.ForceUpdateCanvases();
    // // //     if (contenedorDeJugadores != null)
    // // //     {
    // // //         UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contenedorDeJugadores.GetComponent<RectTransform>());
    // // //     }
    // // // }

    // // // public void ActualizarListaVisual(List<DatosJugadorLobby> jugadores)
    // // // {
    // // //     // Validar contenedor principal
    // // //     if (contenedorDeJugadores == null)
    // // //     {
    // // //         Debug.LogError("[UI] 'contenedorDeJugadores' no está asignado en el Inspector.");
    // // //         return;
    // // //     }

    // // //     // Validar diccionario de avatares por si acaso no fue inicializado en Awake o Start
    // // //     if (avataresPorJugador == null)
    // // //     {
    // // //         avataresPorJugador = new System.Collections.Generic.Dictionary<string, string>();
    // // //     }

    // // //     // Limpiar hijos anteriores de forma segura
    // // //     foreach (Transform child in contenedorDeJugadores)
    // // //     {
    // // //         if (child != null && child.gameObject != null)
    // // //         {
    // // //             Destroy(child.gameObject);
    // // //         }
    // // //     }

    // // //     if (jugadores == null) return;

    // // //     foreach (DatosJugadorLobby jugador in jugadores)
    // // //     {
    // // //         if (jugador == null) continue;

    // // //         if (jugador.id_player == id_player ||
    // // //             (!string.IsNullOrEmpty(nombre_jugador) &&
    // // //              string.Equals(jugador.username, nombre_jugador, System.StringComparison.OrdinalIgnoreCase)))
    // // //         {
    // // //             if (!string.IsNullOrEmpty(jugador.username) && !string.IsNullOrEmpty(jugador.avatar_url))
    // // //                 avataresPorJugador[jugador.username] = jugador.avatar_url;
    // // //             continue;
    // // //         }

    // // //         if (!string.IsNullOrEmpty(jugador.username) && !string.IsNullOrEmpty(jugador.avatar_url))
    // // //             avataresPorJugador[jugador.username] = jugador.avatar_url;

    // // //         // Validar prefab antes de instanciar
    // // //         if (prefabItemJugador == null)
    // // //         {
    // // //             Debug.LogError("[UI] 'prefabItemJugador' no está asignado en el Inspector.");
    // // //             continue;
    // // //         }

    // // //         GameObject nuevoItem = Instantiate(prefabItemJugador, contenedorDeJugadores);
    // // //         if (nuevoItem != null)
    // // //         {
    // // //             jugadorPrefab item = nuevoItem.GetComponent<jugadorPrefab>();
    // // //             if (item != null)
    // // //             {
    // // //                 string idStr = jugador.id_player != null ? jugador.id_player.ToString() : "";
    // // //                 item.Inicializar(idStr, jugador.username, jugador.avatar_url);
    // // //             }
    // // //             else
    // // //             {
    // // //                 Debug.LogWarning("[UI] El prefab instanciado no contiene el componente 'jugadorPrefab'.");
    // // //             }
    // // //         }
    // // //     }

    // // //     // Redibujo seguro de la UI
    // // //     Canvas.ForceUpdateCanvases();
    // // //     RectTransform rectTransform = contenedorDeJugadores.GetComponent<RectTransform>();
    // // //     if (rectTransform != null)
    // // //     {
    // // //         UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    // // //     }
    // // // }



    public void ActualizarListaVisual(List<DatosJugadorLobby> jugadores)
{
    // Respaldo automático del contenedor
    if (contenedorDeJugadores == null)
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (obj.name == "ContenedorJugadores")
            {
                contenedorDeJugadores = obj.transform;
                break;
            }
        }
    }

    // Respaldo automático del prefab del jugador
    if (prefabItemJugador == null)
    {
        // Intenta cargarlo desde una carpeta Resources si la tienes, o búscalo
        prefabItemJugador = Resources.Load<GameObject>("ItemJugador"); // Cambia "ItemJugador" por el nombre exacto de tu prefab si es diferente
        
        if (prefabItemJugador == null)
        {
            // Opcional: buscar entre todos los objetos cargados si estuviera en memoria
            foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (g.name == "ItemJugador" && g.scene.IsValid() == false) // Es un prefab en assets
                {
                    prefabItemJugador = g;
                    break;
                }
            }
        }
    }

    // Validar contenedor principal
    if (contenedorDeJugadores == null)
    {
        Debug.LogError("[UI] 'contenedorDeJugadores' no está asignado ni se pudo encontrar en la escena.");
        return;
    }

    // Validar prefab
    if (prefabItemJugador == null)
    {
        Debug.LogError("[UI] 'prefabItemJugador' no está asignado ni se pudo encontrar automáticamente.");
        return;
    }

    // Validar diccionario de avatares por si acaso no fue inicializado en Awake o Start
    if (avataresPorJugador == null)
    {
        avataresPorJugador = new System.Collections.Generic.Dictionary<string, string>();
    }

    // Limpiar hijos anteriores de forma segura
    foreach (Transform child in contenedorDeJugadores)
    {
        if (child != null && child.gameObject != null)
        {
            Destroy(child.gameObject);
        }
    }

    if (jugadores == null) return;

    foreach (DatosJugadorLobby jugador in jugadores)
    {
        if (jugador == null) continue;

        if (jugador.id_player == id_player ||
            (!string.IsNullOrEmpty(nombre_jugador) &&
             string.Equals(jugador.username, nombre_jugador, System.StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.IsNullOrEmpty(jugador.username) && !string.IsNullOrEmpty(jugador.avatar_url))
                avataresPorJugador[jugador.username] = jugador.avatar_url;
            continue;
        }

        if (!string.IsNullOrEmpty(jugador.username) && !string.IsNullOrEmpty(jugador.avatar_url))
            avataresPorJugador[jugador.username] = jugador.avatar_url;

        GameObject nuevoItem = Instantiate(prefabItemJugador, contenedorDeJugadores);
        if (nuevoItem != null)
        {
            jugadorPrefab item = nuevoItem.GetComponent<jugadorPrefab>();
            if (item != null)
            {
                string idStr = jugador.id_player != null ? jugador.id_player.ToString() : "";
                item.Inicializar(idStr, jugador.username, jugador.avatar_url);
            }
            else
            {
                Debug.LogWarning("[UI] El prefab instanciado no contiene el componente 'jugadorPrefab'.");
            }
        }
    }

    // Redibujo seguro de la UI
    Canvas.ForceUpdateCanvases();
    RectTransform rectTransform = contenedorDeJugadores.GetComponent<RectTransform>();
    if (rectTransform != null)
    {
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }
}


    public void InvitarJugador(string idReceptorDeseado)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var datosInvitacion = new Dictionary<string, object>
        {
            { "idEmisor", id_player.ToString() },
            { "idReceptor", idReceptorDeseado },
            { "nombreEmisor", nombre_jugador }
        };
        EnviarEventoSocket("enviar_invitacion", datosInvitacion);
#else
        if (socket == null || !socket.Connected) return;

        var datosInvitacion = new Dictionary<string, object>
        {
            { "idEmisor", id_player.ToString() },
            { "idReceptor", idReceptorDeseado },
            { "nombreEmisor", nombre_jugador }
        };

        socket.Emit("enviar_invitacion", datosInvitacion);
#endif
    }

    public void SolicitarSiguienteNivel(string nombreSala, int nivelActual)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (string.IsNullOrEmpty(nombreSala) || solicitudSiguienteNivelEnviada) return;
#else
        if (socket == null || string.IsNullOrEmpty(nombreSala) || solicitudSiguienteNivelEnviada) return;
#endif

        solicitudSiguienteNivelEnviada = true;

        int idReceptor = jugadorX == id_player ? jugadorY : jugadorX;
        var datosAEnviar = new
        {
            nombreSala = nombreSala,
            nivelActual = nivelActual,
            idEmisor = id_player,
            idReceptor = idReceptor
        };

        EnviarEventoSocket("solicitar_siguiente_nivel", datosAEnviar);
    }

    public void EnviarEventoSocket(string nombreEvento, object datos)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string jsonDatos = JsonConvert.SerializeObject(datos);
        JS_EmitirEvento(nombreEvento, jsonDatos);
#else
        if (socket != null && socket.Connected)
        {
            socket.Emit(nombreEvento, datos);
        }
#endif
    }

    public bool EstaConectado()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Instance != null;
#else
        return socket != null && socket.Connected;
#endif
    }

    // Punto de entrada invocado desde JavaScript (index.html) en WebGL mediante SendMessage
    public void RecibirEventoSocketWebGL(string jsonMensaje)
    {
        try
        {
            Debug.Log("🔍 JSON bruto recibido en C#: " + jsonMensaje);

            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<MensajeWebGL>(jsonMensaje);
            if (msg != null && !string.IsNullOrEmpty(msg.evento))
            {
                Debug.Log("🌐 Evento puenteado desde JS a C#: " + msg.evento);
                Debug.Log("📦 Contenido de datos (payload): " + msg.datos);

                string payloadJson = msg.datos != null ? msg.datos.ToString() : string.Empty;

                MainThreadDispatcher.Enqueue(() =>
                {
                    if (msg.evento == "iniciar_partida")
                    {
                        Debug.Log("🎯 Entró al bloque de iniciar_partida. Ejecutando...");
                        ProcesarIniciarPartida(payloadJson);
                    }
                    else if (msg.evento == "actualizar_lista_jugadores")
                    {
                        Debug.Log("🎯 Entró al bloque de actualizar_lista_jugadores. Ejecutando...");
                        ProcesarActualizarListaJugadores(payloadJson);
                    }
                });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("🔴 Error al procesar RecibirEventoSocketWebGL: " + e.ToString());
        }
    }
}