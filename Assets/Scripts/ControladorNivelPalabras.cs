using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ControladorNivelPalabras : MonoBehaviour
{
    [Header("Referencias UI Panel Memorización")]
    public GameObject panelMemorizacion;             // Arrastra tu PanelMemorizacion
    public TextMeshProUGUI txtPalabraMemorizacion; // Arrastra txtPalabraMemorizacion
    public Button btnCerrarMemorizacion;             // Arrastra BtnCerrarMemorizacion
    public GameObject panelJuegoTablero;             // El panel o canvas del juego principal

    private List<PalabraItem> listaPalabrasActuales = new List<PalabraItem>();
    private bool estaMemorizando = false;
    private Coroutine rutinaMemorizacionActual;      // Referencia para controlar la corrutina de forma segura

    void Start()
    {
        if (btnCerrarMemorizacion != null)
        {
            btnCerrarMemorizacion.onClick.AddListener(OmitirFaseMemorizacion);
        }

        if (panelMemorizacion != null)
        {
            panelMemorizacion.SetActive(false);
        }
    }

    // Este método se llama cuando el usuario presiona el botón de iniciar nivel
    public void SolicitarPalabrasNivel(int numeroNivel)
    {
        string url = AppConfig.ObtenerUrl($"nivel-palabras/{numeroNivel}");
        StartCoroutine(ObtenerPalabrasDesdeServidor(url));
    }

    IEnumerator ObtenerPalabrasDesdeServidor(string url)
    {
        Debug.Log($"<color=cyan>📡 Consultando URL:</color> {url}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Accept", "application/json");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"<color=green>📥 Respuesta recibida:</color> {jsonResponse}");

                RespuestaNivelPalabras respuesta = JsonConvert.DeserializeObject<RespuestaNivelPalabras>(jsonResponse);

                if (respuesta != null && respuesta.success && respuesta.palabras != null)
                {
                    listaPalabrasActuales = respuesta.palabras;
                    IniciarMemorizacion();
                }
                else
                {
                    Debug.LogError("⚠️ La respuesta del servidor indica éxito falso o la lista está vacía.");
                }
            }
            else
            {
                Debug.LogError($"❌ Error HTTP al conectar con el servidor: {www.error} | Código: {www.responseCode}");
            }
        }
    }

    void IniciarMemorizacion()
    {
        if (panelMemorizacion != null) panelMemorizacion.SetActive(true);
        if (panelJuegoTablero != null) panelJuegoTablero.SetActive(false);

        // Detenemos cualquier rutina anterior de memorización de forma segura
        if (rutinaMemorizacionActual != null)
        {
            StopCoroutine(rutinaMemorizacionActual);
        }

        rutinaMemorizacionActual = StartCoroutine(RutinaMostrarPalabras());
    }

    IEnumerator RutinaMostrarPalabras()
    {
        estaMemorizando = true;

        foreach (var item in listaPalabrasActuales)
        {
            if (!estaMemorizando) break;

            // Mostramos el texto en inglés y su traducción abajo en color cian
            if (txtPalabraMemorizacion != null)
            {
                txtPalabraMemorizacion.text = $"{item.texto}\n<size=60%><color=#00FFFF>{item.traduccion}</color></size>";
            }

            // Reproducimos el audio asociado
            if (!string.IsNullOrEmpty(item.audio))
            {
                ReproducirAudioDeFila(item.audio);
            }

            // Espera 3 segundos antes de mostrar la siguiente palabra
            yield return new WaitForSeconds(3f);
        }

        TerminarMemorizacion();
    }

    public void ReproducirAudioDeFila(string rutaAudioRelativa)
    {
        if (string.IsNullOrEmpty(rutaAudioRelativa)) return;

        string rutaLimpia = rutaAudioRelativa.Replace(".mp3", "").Replace(".wav", "").TrimStart('/');

        if (rutaLimpia.StartsWith("sonidos/"))
        {
            rutaLimpia = rutaLimpia.Substring("sonidos/".Length);
        }

        StartCoroutine(DescargarYReproducirAudio(rutaLimpia));
    }

    IEnumerator DescargarYReproducirAudio(string rutaLimpia)
    {
        AudioClip clip = Resources.Load<AudioClip>(rutaLimpia);
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
        else
        {
            Debug.LogWarning($"⚠️ No se encontró el audio en Resources: [{rutaLimpia}]");
        }
        yield return null;
    }

    public void OmitirFaseMemorizacion()
    {
        estaMemorizando = false;
        
        if (rutinaMemorizacionActual != null)
        {
            StopCoroutine(rutinaMemorizacionActual);
            rutinaMemorizacionActual = null;
        }

        TerminarMemorizacion();
    }

    void TerminarMemorizacion()
    {
        estaMemorizando = false;

        if (panelMemorizacion != null) panelMemorizacion.SetActive(false);
        if (panelJuegoTablero != null) panelJuegoTablero.SetActive(true);

        Debug.Log("<color=yellow>Fase de memorización finalizada. Abriendo tablero de juego...</color>");
        
        // Nota: Aquí puedes notificar a tu GestorTablero para que pinte las fichas del nivel actual si lo requieres.
    }
}

// ==========================================
// ESTRUCTURAS DE DATOS (MODELOS JSON)
// ==========================================
[System.Serializable]
public class PalabraItem
{
    public int id;
    public int nivel;
    public string audio;
    public string texto;
    public string traduccion;
}

[System.Serializable]
public class RespuestaNivelPalabras
{
    public bool success;
    public int nivel;
    public int total_palabras;
    public List<PalabraItem> palabras;
    public string message;
}