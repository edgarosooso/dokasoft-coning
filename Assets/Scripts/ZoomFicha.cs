using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomFicha : MonoBehaviour
{
    private Vector3 escalaOriginal;
    private Vector3 escalaGrande;
    public float factorZoom = 1.4f; 
    public float velocidadZoom = 10f; 

    void Start()
    {
        escalaOriginal = transform.localScale;
        escalaGrande = escalaOriginal * factorZoom;
    }

    // Este método lo puedes llamar desde tu controlador de turnos cuando sea el turno de esta ficha
    public void EjecutarZoomTurno()
    {
        StopAllCoroutines();
        StartCoroutine(AnimarZoomYRegresar());
    }

    IEnumerator AnimarZoomYRegresar()
    {
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * velocidadZoom;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaGrande, tiempo);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * velocidadZoom;
            transform.localScale = Vector3.Lerp(escalaGrande, escalaOriginal, tiempo);
            yield return null;
        }

        transform.localScale = escalaOriginal;
    }
}