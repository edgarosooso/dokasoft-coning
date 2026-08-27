using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; // Usar UnityEngine.UI para UI clásica o UnityEngine.UIElements

public class CargadorAvatar : MonoBehaviour
{
    [Header("Referencia UI")]
    public RawImage avatarRawImage; // Arrastra aquí tu componente RawImage desde el Inspector

    /// <summary>
    /// Método público para iniciar la descarga del avatar pasándole la URL que viene del servidor Node.js
    /// </summary>
    public void CargarAvatarDesdeURL(string urlAvatar)
    {
        if (string.IsNullOrEmpty(urlAvatar))
        {
            Debug.LogWarning("La URL del avatar está vacía.");
            return;
        }

        StartCoroutine(DescargarYAsignarTextura(urlAvatar));
    }

    private IEnumerator DescargarYAsignarTextura(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            // Enviamos la petición y esperamos a que responda el servidor
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error al descargar el avatar desde {url}: {www.error}");
            }
            else
            {
                // Obtenemos la textura descargada exitosamente
                Texture2D texturaAvatar = DownloadHandlerTexture.GetContent(www);

                // La asignamos al componente RawImage en la interfaz
                if (avatarRawImage != null)
                {
                    avatarRawImage.texture = texturaAvatar;
                }
                else
                {
                    Debug.LogWarning("No se ha asignado el componente RawImage en el Inspector.");
                }
            }
        }
    }
}