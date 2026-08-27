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

    [Header("Datos del Usuario Actual")]
    public int id_player;
    public string nombre_jugador;
    public string avatar_url;

    [Header("Configuración Socket")]
    public SocketIOUnity socket;

    [Header("Paneles de Interfaz")]
    public GameObject Panel_Login;
    public GameObject Panel_Lobby;
    public GameObject Panel_Juego;

    [Header("Referencias del Lobby Multijugador")]
    public Transform contenedorDeJugadores; // Usamos tu nombre exacto
    public GameObject prefabItemJugador;    // Usamos tu nombre exacto

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
        // Lógica inicial si la requieres
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
            // Usamos el diccionario Query oficial de SocketIOOptions
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
                // Obtenemos todo el contenido de la respuesta del socket en formato de texto plano
                string rawJson = response.ToString();

                // Si el formato viene como un array envuelto, lo parseamos de manera segura con Newtonsoft
                List<DatosJugadorLobby> jugadores = null;

                // Intentamos deserializar directo o extraer el primer elemento si viene en un array raíz
                try
                {
                    jugadores = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DatosJugadorLobby>>(rawJson);
                }
                catch
                {
                    // Si viene envuelto en un contenedor de SocketIO, usamos un JArray genérico sobre el texto
                    var tokenArray = Newtonsoft.Json.Linq.JArray.Parse(rawJson);
                    if (tokenArray.Count > 0)
                    {
                        jugadores = tokenArray[0].ToObject<List<DatosJugadorLobby>>();
                    }
                }

                if (jugadores != null)
                {
                    Debug.Log($"<color=cyan>Lista de jugadores recibida con éxito. Total:</color> {jugadores.Count}");

                    foreach (var j in jugadores)
                    {
                        Debug.Log($"<color=yellow>DESERIALIZADO -> ID:</color> {j.id_player} | <color=yellow>User:</color> '{j.username}' | <color=yellow>Avatar:</color> '{j.avatar_url}'");
                    }

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
        // 1. Limpiamos el contenedor
        foreach (Transform child in contenedorDeJugadores)
        {
            Destroy(child.gameObject);
        }

        // 2. Instanciamos los prefabs por cada jugador recibido
        foreach (DatosJugadorLobby jugador in jugadores)
        {
            Debug.Log($"Instanciando a -> Usuario: {jugador.username}, Avatar: {jugador.avatar_url}");
            GameObject nuevoItem = Instantiate(prefabItemJugador, contenedorDeJugadores);

            // Usamos tu script real 'jugadorPrefab' y su método 'Inicializar' con los 3 datos
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

        // Empaquetamos los datos que enviará el socket
        var datosInvitacion = new Dictionary<string, object>
        {
            { "idEmisor", id_player.ToString() },      // Tu ID actual
            { "idReceptor", idReceptorDeseado },        // A quién invitas
            { "nombreEmisor", nombre_jugador }          // Tu nombre
        };

        // Emitimos el evento al servidor Node.js
        socket.Emit("enviar_invitacion", datosInvitacion);
        Debug.Log($"Enviando invitación de juego al usuario ID: {idReceptorDeseado}");
    }
}