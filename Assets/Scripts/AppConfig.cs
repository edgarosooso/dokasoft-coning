// using UnityEngine;

// public class AppConfig : MonoBehaviour
// {
//     // URLs base de tu servidor
//     public static string BaseURL = "https://dokasoft.com/dokasoft-coning/api";
//     public static string socketURL = "http://dokasoft.com:3010"; // O tu IP/dominio para producción

//     // Método para obtener cualquier ruta limpiamente
//     public static string ObtenerUrl(string endpoint)
//     {
//         // Limpiamos posibles barras iniciales en el endpoint para evitar duplicados
//         endpoint = endpoint.TrimStart('/');
//         return $"{BaseURL}/{endpoint}";
//     }
// }

using UnityEngine;

public class AppConfig : MonoBehaviour
{
    // Rutas de Producción (APK en el celular)
    private static readonly string ProdBaseURL = "https://dokasoft.com/dokasoft-coning/api";
    private static readonly string ProdSocketURL = "http://dokasoft.com:3010"; 

    // Rutas de Desarrollo (Dándole Play en tu PC)
    private static readonly string DevBaseURL = "https://dokasoft.com/dokasoft-coning/api";
    private static readonly string DevSocketURL = "http://dokasoft.com:3010";

    public static string BaseURL
    {
        get
        {
            return Application.isEditor ? DevBaseURL : ProdBaseURL;
        }
    }

    public static string socketURL
    {
        get
        {
            return Application.isEditor ? DevSocketURL : ProdSocketURL;
        }
    }

    public static string ObtenerUrl(string endpoint)
    {
        endpoint = endpoint.TrimStart('/');
        return $"{BaseURL}/{endpoint}";
    }
}