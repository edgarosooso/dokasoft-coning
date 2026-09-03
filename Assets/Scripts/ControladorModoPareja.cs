using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ControladorModoPareja : MonoBehaviour
{
    [Header("Referencias Ventana aceptar invitacion")]
    public GameObject panelVentanaRecepcion; // El panel con la imagen que acabas de generar
    private string idEmisorActual;            // Guardamos el ID del usuario que nos invitó

    [Header("UI y Referencias Modo Pareja")]
    public GestorTablero gestorTablero;
    public GameObject panelLobby;
    public GameObject panelJuego;

    [Header("Elementos de UI Multijugador")]
    public TextMeshProUGUI textoTurno;
    public TextMeshProUGUI textoPuntajeX;
    public TextMeshProUGUI textoPuntajeY;

    [Header("Referencias de la Ventana de Invitación")]
    public GameObject panelVentanaInvitacion; // Arrastra aquí "VentanaMensaje" desde la jerarquía
    public TextMeshProUGUI textoMensajeInvitacion;
    private string idJugadorDestino;

    void Start()
    {
        StartCoroutine(ConectarEventosParejaConRetraso());
    }

    System.Collections.IEnumerator ConectarEventosParejaConRetraso()
    {
        while (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null)
        {
            yield return null;
        }

        EscucharEventosSocketPareja();
        Debug.Log("Eventos de socket en pareja suscritos correctamente.");
    }

    public void AbrirVentanaInvitacion(string id, string nombre)
    {
        idJugadorDestino = id;
        Debug.Log($"Jugador seleccionado -> Nombre: {nombre}, ID recibido: {id}");

        if (textoMensajeInvitacion != null)
        {
            textoMensajeInvitacion.text = $"¿Deseas enviar una invitación a jugar con {nombre}?";
        }

        if (panelVentanaInvitacion != null)
        {
            panelVentanaInvitacion.SetActive(true);
        }
        else
        {
            Debug.LogError("¡Atención! 'panelVentanaInvitacion' no está asignado en el Inspector de ControladorModoPareja.");
        }
    }

    public void EnviarInvitacionServidor()
    {
        Debug.Log($"Enviando invitación al servidor para el jugador ID: {idJugadorDestino}");

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.socket != null)
        {
            var datosInvitacion = new
            {
                idJugadorEmisor = ControladorJuego.Instance.id_player,
                idJugadorReceptor = idJugadorDestino,
                nombreEmisor = ControladorJuego.Instance.nombre_jugador
            };

            ControladorJuego.Instance.socket.Emit("enviar_invitacion", datosInvitacion);
            CerrarVentanaInvitacion();
        }
        else
        {
            Debug.LogError("No se pudo enviar la invitación: El socket de ControladorJuego es nulo.");
        }
    }

    public void CerrarVentanaInvitacion()
    {
        Debug.Log("Cerrando ventana de invitación...");

        if (panelVentanaInvitacion != null)
        {
            panelVentanaInvitacion.SetActive(false);
            Debug.Log("Ventana cerrada con éxito.");
        }
    }

    public void MostrarVentanaRecepcion(string idEmisor, string nombreEmisor)
    {
        idEmisorActual = idEmisor;

        if (panelVentanaRecepcion != null)
        {
            panelVentanaRecepcion.SetActive(true);
        }
    }

    public void AceptarInvitacionRemota()
    {
        Debug.Log($"Aceptando invitación del emisor ID: {idEmisorActual}");

        if (ControladorJuego.Instance != null && ControladorJuego.Instance.socket != null)
        {
            // Activamos la bandera global de multijugador aquí
            ControladorJuego.Instance.esModoMultijugador = true;

            // Guardamos el ID del emisor como la sala actual (ajusta esto si tu servidor usa otro nombre de sala)
            ControladorJuego.Instance.nombreSalaActual = idEmisorActual;

            var datosAceptacion = new
            {
                idEmisor = idEmisorActual,
                idReceptor = ControladorJuego.Instance.id_player
            };

            ControladorJuego.Instance.socket.Emit("aceptar_invitacion", datosAceptacion);

            if (panelVentanaRecepcion != null)
                panelVentanaRecepcion.SetActive(false);
        }
    }

    public void RechazarInvitacionRemota()
    {
        Debug.Log("Invitación rechazada.");

        if (panelVentanaRecepcion != null)
        {
            panelVentanaRecepcion.SetActive(false);
        }
    }

    void EscucharEventosSocketPareja()
    {
        if (ControladorJuego.Instance == null || ControladorJuego.Instance.socket == null) return;

        // 1. Evento de ficha volteada por el rival
        ControladorJuego.Instance.socket.OnUnityThread("ficha_volteada_remota", (response) =>
        {
            try
            {
                Debug.Log("--- ficha_volteada_remota ---");
                string rawJson = response != null ? response.ToString() : "";
                int indiceFichaRemota = -1;

                var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
                if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                {
                    token = array[0];
                }

                if (token is Newtonsoft.Json.Linq.JObject obj && obj["indiceFicha"] != null)
                {
                    // Cambiado aquí para evitar el error de sintaxis:
                    indiceFichaRemota = obj["indiceFicha"].ToObject<int>();
                }

                UnityThread.executeInUpdate(() =>
                {
                    if (gestorTablero != null && gestorTablero.contenedorMatriz != null && indiceFichaRemota >= 0 && indiceFichaRemota < gestorTablero.contenedorMatriz.childCount)
                    {
                        Transform fichaTransform = gestorTablero.contenedorMatriz.GetChild(indiceFichaRemota);
                        if (fichaTransform != null)
                        {
                            ControladorFicha fichaRemota = fichaTransform.GetComponent<ControladorFicha>();
                            if (fichaRemota != null)
                            {
                                fichaRemota.RevelarFichaRemota();

                                if (gestorTablero != null)
                                {
                                    gestorTablero.FichaSeleccionada(fichaRemota);
                                }
                            }
                        }
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'ficha_volteada_remota': " + e.ToString());
            }
        });

       
        ControladorJuego.Instance.socket.OnUnityThread("actualizar_estado_partida", (response) =>
        {
            try
            {
                Debug.Log("Actualizando estado de la partida en pareja...");
                string rawJson = response != null ? response.ToString() : "";

                var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);
                if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                {
                    token = array[0];
                }

                if (token is Newtonsoft.Json.Linq.JObject obj)
                {
                    // Actualizamos el turno actual con lo que mande el servidor
                    if (obj["turnoActual"] != null)
                    {
                        ControladorJuego.Instance.turnoActual = obj["turnoActual"].ToString();
                        Debug.Log("Nuevo turno asignado al socket ID: " + ControladorJuego.Instance.turnoActual);
                    }

                    // Opcional: Si también mandas puntajes en este evento, puedes actualizarlos aquí en tu UI
                    // string puntajeX = obj["puntajeX"]?.ToString();
                    // string puntajeY = obj["puntajeY"]?.ToString();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'actualizar_estado_partida': " + e.ToString());
            }
        });

        // 4. Evento de recepción de invitación
        ControladorJuego.Instance.socket.OnUnityThread("recibir_invitacion", (response) =>
        {
            try
            {
                string rawJson = response != null ? response.ToString() : "";
                Debug.Log("JSON recibido en 'recibir_invitacion': " + rawJson);

                string idEmisor = "";
                string nombreEmisor = "";

                var token = Newtonsoft.Json.Linq.JToken.Parse(rawJson);

                if (token is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                {
                    token = array[0];
                }

                if (token is Newtonsoft.Json.Linq.JArray innerArray && innerArray.Count > 0)
                {
                    token = innerArray[0];
                }

                if (token is Newtonsoft.Json.Linq.JObject obj)
                {
                    idEmisor = obj["idEmisor"]?.ToString();
                    nombreEmisor = obj["nombreEmisor"]?.ToString();
                }

                Debug.Log($"¡Invitación procesada con éxito de {nombreEmisor} (ID: {idEmisor})!");

                MostrarVentanaRecepcion(idEmisor, nombreEmisor);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al procesar 'recibir_invitacion': " + e.ToString());
            }
        });
    }
}