using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Solo cargamos la librería de C# si NO estamos en WebGL
#if !(UNITY_WEBGL && !UNITY_EDITOR)
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
#endif

public class TestSocketDirecto : MonoBehaviour
{
    // Declaraciones para el puente nativo en WebGL
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void JS_ConectarSocket(string url, string path, string idPlayer);

    [DllImport("__Internal")]
    private static extern void JS_DesconectarSocket();
    #else
    // Instancia para Android / PC / Editor
    private SocketIOUnity socketActual;

    private void CerrarSocketAnterior()
    {
        if (socketActual != null)
        {
            try
            {
                socketActual.Disconnect();
                socketActual.Dispose();
            }
            catch { }
            socketActual = null;
        }
    }
    #endif

    public void ConectarServidor()
    {
        string url = "https://dokasoft.com";
        string path = "/coning/socket.io/";
        string idPlayer = "1";

        #if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Lanzando puente nativo JavaScript para WebGL...");
        JS_ConectarSocket(url, path, idPlayer);
        #else
        Debug.Log("Iniciando conexión estándar para Android/PC...");
        ConectarAndroidPC(url, path, idPlayer);
        #endif
    }

    // Método exclusivo para Android / PC / Editor
    private async void ConectarAndroidPC(string url, string path, string idPlayer)
    {
        #if !(UNITY_WEBGL && !UNITY_EDITOR)
        CerrarSocketAnterior();

        var uri = new Uri(url);

        socketActual = new SocketIOUnity(uri, new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Path = path,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            Query = new Dictionary<string, string> { { "id_player", idPlayer } }
        });

        socketActual.JsonSerializer = new NewtonsoftJsonSerializer();

        socketActual.OnConnected += (sender, e) =>
        {
            Debug.Log("<color=green>¡ÉXITO! Socket conectado correctamente en Android/PC.</color>");
        };

        socketActual.OnError += (sender, e) =>
        {
            Debug.LogError($"[ERROR DE SOCKET]: {e}");
        };

        socketActual.OnDisconnected += (sender, e) =>
        {
            Debug.LogWarning("[DESCONECTADO DEL SERVIDOR]");
        };

        try
        {
            await socketActual.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Excepción capturada al conectar: {ex.Message}");
        }
        #endif
    }

    // Métodos de respuesta llamados por JavaScript desde el navegador (WebGL)
    public void OnSocketConnectedJS()
    {
        Debug.Log("<color=green>¡ÉXITO! Socket conectado correctamente a través de IIS (WebGL).</color>");
    }

    public void OnSocketDisconnectedJS()
    {
        Debug.LogWarning("[DESCONECTADO DEL SERVIDOR WEBGL]");
    }

    private void OnDestroy()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        JS_DesconectarSocket();
        #else
        CerrarSocketAnterior();
        #endif
    }
}