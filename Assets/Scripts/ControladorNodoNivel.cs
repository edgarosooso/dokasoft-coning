using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;



public class ControladorNodoNivel : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoNumeroNivel; // Arrastra aquí el texto TMP del nivel correspondiente

    public GameObject PanelNiveles;
    public GameObject panelLobby;
    private int numeroNivel;


    void Start()
    {

        PanelNiveles = GameObject.Find("PanelNivel");
        panelLobby = BuscarObjetoInactivo("Panel_Lobby");
        



        // Opcional: leer automáticamente el número desde el componente de texto si ya está escrito
        if (textoNumeroNivel != null && int.TryParse(textoNumeroNivel.text, out int nivelParsed))
        {
            numeroNivel = nivelParsed;
        }


    }

    // Método auxiliar para encontrar objetos inactivos en la escena
    GameObject BuscarObjetoInactivo(string nombre)
    {
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            // Verificamos que sea un objeto de la escena actual y no un prefab del proyecto
            if (obj.name == nombre && obj.scene.IsValid())
            {
                return obj;
            }
        }
        return null;
    }

    // Este método se asigna al evento OnClick() del botón en Unity
    public void AlHacerClicEnNivel()
    {
        if (int.TryParse(textoNumeroNivel.text, out numeroNivel))
        {

            Debug.Log("Nivel seleccionado: " + numeroNivel);

            // 1. Guardar el nivel seleccionado globalmente (opcional si MenuNiveles ya lo maneja)
            PlayerPrefs.SetInt("NivelActual", numeroNivel);
            PlayerPrefs.Save();

            PanelNiveles.SetActive(false);


            if (panelLobby != null)
            {
                Debug.Log("activar lobby true");
                panelLobby.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No se encontró el Panel_Lobby. Asegúrate de que exista en la escena.");
            }




            // 2. Llamar al método público SeleccionarNivel de la clase MenuNiveles
            // Asegúrate de que MenuNiveles tenga una instancia o referencia estática (Instance) o búscalo en escena.
            if (MenuNiveles.Instance != null)
            {
                MenuNiveles.Instance.SeleccionarNivel(numeroNivel);
            }
            else
            {
                Debug.LogError("No se encontró la instancia de MenuNiveles en la escena.");
            }
        }
        else
        {
            Debug.LogWarning("El texto del nivel no es un número válido.");
        }
    }
}