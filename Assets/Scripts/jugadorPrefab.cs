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
    // (Añadimos 'string id' para recibir también el identificador único)
    public void Inicializar(string id, string nombre, string avatarUrl)
    {
        idJugadorActual = id;
        nombreJugadorActual = nombre;

        if (textoNombre != null)
        {
            textoNombre.text = nombre;
        }

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            StartCoroutine(DescargarAvatar(avatarUrl));
        }
    }

    // Este es el método que se ejecutará cuando hagas clic en este renglón de la lista
    public void AlHacerClicEnJugador()
    {
        Debug.Log($"Hizo clic en el jugador: {nombreJugadorActual}");

        // Buscamos el gestor de invitaciones en la escena
        ControladorInvitaciones gestor = FindObjectOfType<ControladorInvitaciones>();

        if (gestor != null)
        {
            // Abrimos la ventana pasándole su ID y su nombre real
            gestor.AbrirVentanaInvitacion(idJugadorActual, nombreJugadorActual);
        }
        else
        {
            Debug.LogWarning("No se encontró el componente ControladorInvitaciones en la escena.");
        }
    }

    private IEnumerator DescargarAvatar(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D textura = DownloadHandlerTexture.GetContent(www);
                if (imagenAvatar != null)
                {
                    imagenAvatar.texture = textura;
                }
            }
            else
            {
                Debug.LogWarning($"No se pudo descargar el avatar desde {url}: {www.error}");
            }
        }
    }
}