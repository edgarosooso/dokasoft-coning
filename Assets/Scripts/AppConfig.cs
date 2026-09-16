using UnityEngine;

public class AppConfig : MonoBehaviour
{
    // Rutas de Producción (APK en el celular)
    
    private static readonly string ProdBaseURL = "https://dokasoft.com/coning/api";
    private static readonly string ProdSocketURL = "https://dokasoft.com/coning/socket.io";
    
// O asegurarte de que el cliente de Socket.io en C# use la ruta base correcta del namespace si aplica.

    //private static readonly string ProdSocketURL = "https://dokasoft.com";
 
    

    // Rutas de Desarrollo (Dándole Play en tu PC)
    private static readonly string DevBaseURL = "https://dokasoft.com/coning/api";
    //private static readonly string DevSocketURL = "http://dokasoft.com:3010";
    private static readonly string DevSocketURL = "https://dokasoft.com";

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