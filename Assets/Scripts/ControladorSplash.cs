using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ControladorSplash : MonoBehaviour
{
    [Header("Configuración del Splash")]
    public float segundosDeEspera = 3.0f; // Tiempo que se mostrará la imagen
    public string nombreEscenaMenu = "SampleScene"; // Tu escena principal donde está el Lobby

    void Start()
    {
        StartCoroutine(EsperarYCargarCo());
    }

    private IEnumerator EsperarYCargarCo()
    {
        // Espera los segundos configurados en pantalla
        yield return new WaitForSeconds(segundosDeEspera);
        
        // Carga la siguiente escena
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}