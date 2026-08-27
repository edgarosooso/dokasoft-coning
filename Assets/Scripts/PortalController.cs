using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Importante para detectar el clic mantenido

public class PortalController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias")]
    public Image aroProgreso;
    public GameObject textoAviso;

    [Header("Configuración")]
    public float tiempoRequerido = 2.0f;

    private float timer = 0f;
    private bool estaListo = false;
    private bool estaPresionando = false;
    private bool presionando = false;
    // --- LLAMA A ESTO CUANDO TERMINEN LOS PODERES ---
    public void HabilitarPortal()
    {
        this.gameObject.SetActive(true); // Se asegura de que el aro sea visible
        estaListo = true;
        if (textoAviso != null) textoAviso.SetActive(true);
    }

    void Update()
    {
        if (!estaListo) return;

        if (estaPresionando)
        {
            timer += Time.deltaTime;
            aroProgreso.fillAmount = timer / tiempoRequerido;

            if (timer >= tiempoRequerido)
            {
                EjecutarInicioJuego();
            }
        }
        else
        {
            // Si suelta, la barra se vacía suavemente
            timer = Mathf.Max(0, timer - Time.deltaTime * 2f);
            aroProgreso.fillAmount = timer / tiempoRequerido;
        }
    }

    // Detecta cuando el Admin pone el dedo/mouse
    public void OnPointerDown(PointerEventData eventData)
    {
        if (estaListo) estaPresionando = true;
    }

    // Detecta cuando el Admin lo quita
    public void OnPointerUp(PointerEventData eventData)
    {
        estaPresionando = false;
    }

 private void EjecutarInicioJuego()
{
    // 1. Evitamos que se ejecute varias veces
    estaListo = false; 
    estaPresionando = false;

    // --- NUEVO: LÓGICA DE AUDIO ---
    AudioSource fuenteAudio = GetComponent<AudioSource>();
    if (fuenteAudio != null && fuenteAudio.clip != null)
    {
        // Reproduce el clip en la posición de la cámara para que se escuche perfecto
        // Esto crea un objeto temporal que no se destruye al apagar el portal
        AudioSource.PlayClipAtPoint(fuenteAudio.clip, Camera.main.transform.position);
    }
    // ------------------------------

    Debug.Log("<color=cyan>Portal: ¡Carga completa detectada!</color>");

    // 2. Llamada al ControladorJuego
    if (ControladorJuego.Instance != null)
    {
        Debug.Log("<color=cyan>Portal: Llamando a Empezar_A_Jugar en el Gestor...</color>");
     //   ControladorJuego.Instance.Empezar_A_Jugar();
    }
    else
    {
        Debug.LogError("¡ERROR! No se encontró la 'Instance' en ControladorJuego. Revisa el Awake.");
    }

    // 3. AUTO-OCULTADO
    this.gameObject.SetActive(false);
}

   void FinalizarCarga() {
    presionando = false;
    estaListo = false; 

    // REVISA ESTA LÍNEA: 
    // Si usaste 'Instance' con mayúscula, debe ser así:
    // if (ControladorJuego.Instance != null) {
    //     Debug.Log("Aro lleno: Avisando al ControladorJuego"); // Si este sale y el otro no, el problema es el Controlador
    //     ControladorJuego.Instance.Empezar_A_Jugar(); 
    // } else {
    //     Debug.LogError("¡ERROR! No se encontró la Instancia del ControladorJuego");
    // }

    // Opcional: Apagarse a sí mismo para limpiar la pantalla
    this.gameObject.SetActive(false);
}
}