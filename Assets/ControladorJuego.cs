// Fecha de creación: 25 de enero de 2026
using System;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json;

public class ControladorJuego : MonoBehaviour
{
    public static ControladorJuego Instance;

    [Header("Datos de Sala Multijugador")]
    public string nombreSalaActual;
    public string idSalaActual;
    public string turnoActual;

    [Header("Datos del Usuario Actual")]
    public int id_player;
    public string nombre_jugador;
    public string avatar_url;

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
        MostrarSoloLogin();
    }

    void Start()
    {
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
                    string nuevoTurno = null; // 👈 Variable para capturar el turno que manda el servidor

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

                        // 👈 Capturamos el turno siguiente que viene del servidor (puede llamarse turnoActual, siguienteTurno o turno)
                        if (obj["turnoActual"] != null)
                        {
                            nuevoTurno = obj["turnoActual"].ToString();
                        }
                        else if (obj["siguienteTurno"] != null)
                        {
                            nuevoTurno = obj["siguienteTurno"].ToString();
                        }
                    }

                    if (!string.IsNullOrEmpty(nuevoTurno))
                    {
                        // 👈 Actualizamos el turno global en el controlador
                        ControladorJuego.Instance.turnoActual = nuevoTurno;
                        Debug.Log($"🔄 Turno actualizado por el servidor tras acierto: [{nuevoTurno}]");
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
}