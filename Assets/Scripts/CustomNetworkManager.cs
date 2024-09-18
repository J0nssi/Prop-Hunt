using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;
using static PlayerObjectController;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private PlayerObjectController GamePlayerPrefab;
    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if(SceneManager.GetActiveScene().name == "Lobby")
        {
            PlayerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);
            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count);

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    public void StartGame(string SceneName)
    {
        AssignRoles();
        ServerChangeScene(SceneName);
    }
    private void AssignRoles()
    {
        // Ensure there's at least one player
        if (GamePlayers.Count == 0) return;

        // Randomly pick one player to be the Hunter
        int hunterIndex = Random.Range(0, GamePlayers.Count);

        for (int i = 0; i < GamePlayers.Count; i++)
        {
            if (i == hunterIndex)
            {
                GamePlayers[i].Role = PlayerRole.Hunter;
            }
            else
            {
                GamePlayers[i].Role = PlayerRole.Prop;
            }
        }
    }
}
