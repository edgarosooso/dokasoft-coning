mergeInto(LibraryManager.library, {
  JS_ConectarSocket: function (urlStr, pathStr, idPlayerStr) {
    var url = UTF8ToString(urlStr);
    var path = UTF8ToString(pathStr);
    var idPlayer = UTF8ToString(idPlayerStr);

    console.log("[JSBridge] Verificando conexión global para el jugador:", idPlayer);

    if (typeof io === 'undefined') {
      console.error("[JSBridge] La librería socket.io no está cargada en la página HTML.");
      return;
    }

    // Si ya existe un socket pero cambió de jugador o queremos asegurar frescura, lo reiniciamos
    if (window.socket) {
      console.log("[JSBridge] Cerrando socket anterior para reconectar con ID fresco.");
      window.socket.disconnect();
      window.socket = null;
    }

    // Creamos la conexión limpia asegurando el id_player actual
    window.socket = io(url, {
      path: path,
      transports: ['websocket', 'polling'],
      query: { id_player: idPlayer },
      forceNew: true
    });

    // ESCUCHADOR GLOBAL DIRECTO AQUÍ (Para garantizar que atrape todo y lo mande a Unity)
    window.socket.onAny(function (event, ...args) {
      console.log("📥 [1. JSBridge] Evento recibido del servidor:", event, args);
      try {
        var payloadDatos = args[0];
        if (typeof payloadDatos === "object") {
          payloadDatos = JSON.stringify(payloadDatos);
        }

        // Buscamos la instancia de Unity de forma segura
        var unityInstance = window.myUnityInstance || window.unityInstance;
        if (unityInstance) {
          unityInstance.SendMessage("Gestor", "RecibirEventoSocketWebGL", JSON.stringify({ 
            evento: event, 
            datos: payloadDatos 
          }));
          console.log("🚀 [2. JSBridge] Reenviado a Unity exitosamente:", event);
        } else {
          console.error("❌ [JSBridge Error] No se encontró la instancia de Unity para enviar:", event);
        }
      } catch (e) {
        console.error("❌ [JSBridge Catch Error] Error procesando evento en puente:", e);
      }
    });

    window.socket.on('connect', function () {
      console.log("🟢 [JSBridge] Conectado exitosamente, ID del socket:", window.socket.id);
    });

    window.socket.on('disconnect', function () {
      console.log("[JSBridge] Desconectado del servidor.");
    });

    window.socket.on('connect_error', function (err) {
      console.error("[JSBridge] Error de conexión: ", err);
    });
  },

  JS_DesconectarSocket: function () {
    if (window.socket) {
      window.socket.disconnect();
      window.socket = null;
    }
  },
  
  JS_EmitirEvento: function (eventoStr, datosJsonStr) {
    var evento = UTF8ToString(eventoStr);
    var datosJson = UTF8ToString(datosJsonStr);
    var datos = datosJson ? JSON.parse(datosJson) : {};

    var targetSocket = window.socket || (window.parent ? window.parent.socket : null);

    if (targetSocket && targetSocket.connected) {
      targetSocket.emit(evento, datos);
      console.log("[JSBridge] Evento emitido con éxito al servidor: " + evento, datos);
    } else {
      console.error("[JSBridge] ERROR CRÍTICO: No se encontró window.socket conectado para emitir:", evento);
    }
  }
});