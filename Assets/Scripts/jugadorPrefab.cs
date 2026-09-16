using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class jugadorPrefab : MonoBehaviour
{
    public TextMeshProUGUI textoNombre;
    public RawImage imagenAvatar;

    private string idJugadorActual;     // Guardamos el ID del jugador de este renglón
    private string nombreJugadorActual; // Guardamos el nombre del jugador de este renglón

    // Método que se llama desde el administrador del lobby para configurar cada ranura
    public void Inicializar(string id, string nombre, string avatarUrl)
    {
        idJugadorActual = id;
        nombreJugadorActual = nombre;

        if (textoNombre != null)
        {
            textoNombre.text = nombre;
        }

        // Normalizamos la URL antes de intentar descargarla
        string urlNormalizada = NormalizarUrlAvatar(avatarUrl);

        if (!string.IsNullOrEmpty(urlNormalizada) && gameObject.activeInHierarchy)
        {
            StartCoroutine(DescargarAvatar(urlNormalizada));
        }
    }

    // Este es el método que se ejecutará cuando hagas clic en este renglón de la lista
    public void AlHacerClicEnJugador()
    {
        Debug.Log($"Hizo clic en el jugador: {nombreJugadorActual} con ID: {idJugadorActual}");

        // Buscamos el ControladorModoPareja en lugar del de invitaciones
        ControladorModoPareja gestorPareja = Object.FindFirstObjectByType<ControladorModoPareja>();

        if (gestorPareja != null)
        {
            // Abrimos la ventana pasándole su ID y su nombre real
            gestorPareja.AbrirVentanaInvitacion(idJugadorActual, nombreJugadorActual);
        }
        else
        {
            Debug.LogWarning("No se encontró el componente ControladorModoPareja en la escena.");
        }
    }

    private string NormalizarUrlAvatar(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim();
        
        // Si ya viene con http o https, lo limpiamos de prefijos incorrectos si los tuviera, o lo devolvemos
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            // Si por alguna razón la URL absoluta trae el segmento viejo de IIS, lo corregimos al vuelo:
            if (url.Contains("/dokasoft-coning/imagenes/avatar/"))
            {
                url = url.Replace("/dokasoft-coning/imagenes/avatar/", "/imagenes/avatar/");
            }
            return url;
        }
        
        // Si empieza con barra, completamos con el dominio base
        if (url.StartsWith("/")) return "https://dokasoft.com" + url;
        
        // Forzamos a que siempre busque en la ruta limpia del proxy que mapeamos en IIS
        return "https://dokasoft.com/imagenes/avatar/" + url.TrimStart('/');
    }

    private IEnumerator DescargarAvatar(string url)
    {
        Debug.Log("🔍 [Prefab] Intentando descargar avatar normalizado: " + url);

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            // Verificamos que el GameObject siga activo en la escena antes de procesar el resultado
            if (!this || !gameObject.activeInHierarchy) yield break;

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D textura = DownloadHandlerTexture.GetContent(www);
                if (imagenAvatar != null && textura != null)
                {
                    imagenAvatar.texture = textura;
                    Debug.Log("🟢 [Prefab] Avatar descargado con éxito desde: " + url);
                }
            }
            else
            {
                Debug.LogWarning($"🔴 [Prefab] No se pudo descargar el avatar desde {url}: {www.error}");
            }
        }
    }
}