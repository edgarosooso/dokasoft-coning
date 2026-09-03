using System.Collections.Generic;

[System.Serializable]
public class DatosPartidaRespuesta
{
    public string turnoActual;
    public string nombreSala;
    public int nivel;
    public int total_parejas;
    public List<ItemNivelRespuesta> fichas;
    public List<ItemNivelRespuesta> configuracion;
    public string nombreJugadorX; 
    public string nombreJugadorY; 
    public int jugadorX;
    public int jugadorY;
}

[System.Serializable]
public class ItemNivelRespuesta
{
    public int id;
    public int fichaid;
    public string texto;
    public string audio;
    public string ruta_audio;
    public string traduccion;
    public int indice_posicion;
}
