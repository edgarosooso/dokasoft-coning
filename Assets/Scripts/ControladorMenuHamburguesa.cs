using UnityEngine;

public class ControladorMenuHamburguesa : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El panel o contenedor que se va a desplegar.")]
    public GameObject panelDesplegable;

    private bool estaAbierto = false;

    void Start()
    {
        // Asegurarnos de que el menú comience cerrado al iniciar la escena
        if (panelDesplegable != null)
        {
            panelDesplegable.SetActive(false);
        }
    }

    // Método para alternar entre abierto y cerrado
    public void AlternarMenu()
    {
        estaAbierto = !estaAbierto;

        if (panelDesplegable != null)
        {
            panelDesplegable.SetActive(estaAbierto);
        }
    }

    // Métodos opcionales por si quieres abrirlos o cerrarlos de forma explícita
    public void AbrirMenu()
    {
        estaAbierto = true;
        if (panelDesplegable != null) panelDesplegable.SetActive(true);
    }

    public void CerrarMenu()
    {
        estaAbierto = false;
        if (panelDesplegable != null) panelDesplegable.SetActive(false);
    }
}