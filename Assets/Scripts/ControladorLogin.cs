using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ControlLogin : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_InputField campoUsuario;
    public TMP_InputField campoPassword;
    public GameObject panelLogin;
    public GameObject lobbyJuego;

    public GameObject sistemaLuciernagas; // Arrastra el objeto "luciernagas" aquí en el Inspector

    [Header("Configuración Servidor")]
    public string urlApi = "";

    void Awake()
    {
        // Usamos AppConfig para centralizar la ruta
        urlApi = AppConfig.ObtenerUrl("login");
    }

    public void IniciarSesion()
    {
        Debug.Log("INICAR SESION.");
        Debug.Log("Botón presionado");
        if (campoUsuario.text != "" && campoPassword.text != "")
        {
            StartCoroutine(EnviarLoginAlServidor(campoUsuario.text, campoPassword.text));
        }
        else
        {
            Debug.Log("Por favor, rellena los datos.");
        }
    }

    IEnumerator EnviarLoginAlServidor(string usuario, string clave)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", usuario);
        form.AddField("password", clave);

        Debug.Log($"<color=green>direccion :</color> {urlApi}");

        using (UnityWebRequest www = UnityWebRequest.Post(urlApi, form))
        {
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;

                // Usamos Newtonsoft.Json para leer perfectamente el objeto anidado
                LoginResponse respuesta = JsonConvert.DeserializeObject<LoginResponse>(jsonResponse);

                if (respuesta != null && respuesta.success && respuesta.user != null)
                {
                    Debug.Log($"<color=green>Login exitoso:</color> {respuesta.user.username}");
                    Debug.Log($"<color=green>ID PLAYER:</color> {respuesta.user.id_player}");
                    Debug.Log($"<color=green>AVATAR URL:</color> {respuesta.user.avatar_url}");

                    // --- GUARDAR CREDENCIALES LOCALMENTE ---
                    PlayerPrefs.SetString("UsuarioGuardado", usuario);
                    PlayerPrefs.SetString("PasswordGuardado", campoPassword.text);
                    PlayerPrefs.SetString("token", respuesta.token);
                    PlayerPrefs.Save();

                    // --- ASIGNAMOS LOS DATOS AL CONTROLADOR DE JUEGO UNA SOLA VEZ ---
                    if (ControladorJuego.Instance != null)
                    {
                        ControladorJuego.Instance.gameObject.SetActive(true);
                        ControladorJuego.Instance.id_player = respuesta.user.id_player;
                        ControladorJuego.Instance.nombre_jugador = respuesta.user.username;
                        ControladorJuego.Instance.avatar_url = respuesta.user.avatar_url;
                    }
                    else
                    {
                        Debug.LogWarning("ControladorJuego.Instance no está presente en esta escena.");
                    }

                    // Apagamos las partículas al entrar
                    if (sistemaLuciernagas != null)
                    {
                        sistemaLuciernagas.SetActive(false);
                    }

                    // Pasamos el ID real directo a la transición del juego y socket
                    EntrarAlJuego(respuesta.user.id_player.ToString());
                }
                else
                {
                    Debug.LogError("Error en los datos de respuesta o usuario nulo.");
                }
            }
            else
            {
                Debug.LogError("Error en el servidor: " + www.error);
            }
        }
    }

    void Start()
    {
        // Verificamos si existen datos guardados previamente
        if (PlayerPrefs.HasKey("UsuarioGuardado"))
        {
            campoUsuario.text = PlayerPrefs.GetString("UsuarioGuardado");
        }

        if (PlayerPrefs.HasKey("PasswordGuardado"))
        {
            campoPassword.text = PlayerPrefs.GetString("PasswordGuardado");
        }
    }

    void EntrarAlJuego(string id_player)
    {
        panelLogin.SetActive(false); // Apagas el login
        lobbyJuego.SetActive(true);  // Prendes el visual del lobby

        // --- AQUÍ PRENDES EL GESTOR ---
        GameObject gestorLobby = GameObject.Find("Gestor_Lobby");
        if (gestorLobby != null)
        {
            gestorLobby.SetActive(true);
            Debug.Log("<color=yellow>Gestor Lobby despertado después del Login.</color>");
        }

        // --- ÚNICO PUNTO DE CONEXIÓN AL SOCKET CON EL ID REAL ---
        if (ControladorJuego.Instance != null)
        {
            Debug.Log($"<color=cyan>Conectando socket con ID real:</color> {id_player}");
            ControladorJuego.Instance.ConfigurarSocket(id_player);
        }
    }
}