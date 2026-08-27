[System.Serializable]
public class UsuarioData {
    public int id_player;
    public string username;
    public string saldo_disponible;
    public string avatar_url;
}

[System.Serializable]
public class LoginResponse {
    public bool success;
    public UsuarioData user;
    public string token;
}