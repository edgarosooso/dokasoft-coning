using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomFicha : MonoBehaviour
{
    private Vector3 escalaOriginal;
    private Vector3 escalaGrande;
    
    [Header("Configuración de Tamaño y Tiempo")]
    [Tooltip("Factor de zoom. Ponlo en 2.5 o 3 para que ocupe gran parte de la pantalla.")]
    public float factorZoom = 2.8f; 
    public float velocidadZoom = 12f;
    
    [Tooltip("Segundos que la tarjeta se quedará en grande para que el usuario alcance a estudiar.")]
    public float tiempoDeEstudio = 5f; 

    private bool estaEnZoom = false;

    void Start()
    {
        escalaOriginal = transform.localScale;
        escalaGrande = escalaOriginal * factorZoom;
    }

    // Este método se ejecuta cuando hacen clic en la ficha
    public void EjecutarZoomTurno()
    {
        if (estaEnZoom) return;
        StopAllCoroutines();
        StartCoroutine(AnimarZoomYEstudiar());
    }

    IEnumerator AnimarZoomYEstudiar()
    {
        estaEnZoom = true;

        // 1. Asegurar que la tarjeta se dibuje por encima de todas las demás
        transform.SetAsLastSibling();

        // 2. Animación para CRECER suavemente al tamaño grande
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * velocidadZoom;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaGrande, tiempo);
            yield return null;
        }
        transform.localScale = escalaGrande;

        // 3. TIEMPO DE ESTUDIO: La tarjeta se queda grande el tiempo configurado (ej. 5 segundos)
        // Opcional: Aquí el usuario puede leer tranquilamente la traducción, inglés y fonética.
        float temporizador = 0;
        while (temporizador < tiempoDeEstudio)
        {
            // Si el usuario decide tocar antes o la ficha se destruye/oculta, podemos romper el ciclo si es necesario
            temporizador += Time.deltaTime;
            yield return null;
        }

        // 4. Animación para REDUCIRSE suavemente de regreso a su tamaño normal
        tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * velocidadZoom;
            transform.localScale = Vector3.Lerp(escalaGrande, escalaOriginal, tiempo);
            yield return null;
        }
        
        transform.localScale = escalaOriginal;
        estaEnZoom = false;
    }
}