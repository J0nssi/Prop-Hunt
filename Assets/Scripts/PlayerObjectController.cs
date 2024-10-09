using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;

public class PlayerObjectController : NetworkBehaviour
{
    //Player Data
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;

    //Cameras
    public GameObject ThirdPersonCamera;
    public GameObject FirstPersonCamera;
    public GameObject EmptyCamera;

    public GameObject[] propPrefabs; // Array to hold multiple prop model prefabs
    private GameObject propInstance; // To hold the instantiated prop model


    //Roles
    public enum PlayerRole
    {
        Hunter,
        Prop
    }

    [SyncVar(hook = nameof(OnRoleChanged))]
    public PlayerRole Role;


    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        Debug.Log($"Role changed from {oldRole} to {newRole}");
        // Only instantiate or switch models on the local player if it's their role
        if (isLocalPlayer)
        {
            HandleRoleChange(newRole); // Camera switching handled here
        }

        RpcUpdateRole(newRole);
    }

    [Command]
    public void CmdSetRole(PlayerRole newRole)
    {
        Debug.Log($"CmdSetRole: Setting role to {newRole} for {gameObject.name}");

        // Server sets the role
        if (isServer)
        {
            Role = newRole;
            Debug.Log($"Server assigned role {newRole} to {gameObject.name}");

            // Notify all clients about the role change
            RpcUpdateRole(newRole);
        }
    }

    [ClientRpc]
    void RpcUpdateRole(PlayerRole newRole)
    {
        // Only instantiate or switch models on the local player if it's their role
        if (isLocalPlayer)
        {
            HandleRoleChange(newRole); // Camera switching handled here
        }
    }

    private void HandleRoleChange(PlayerRole newRole)
    {
        if (!isLocalPlayer) return;  // Only handle camera switching for the local player

        Debug.Log($"Switching cameras for {gameObject.name} based on role: {newRole}");

        if (newRole == PlayerRole.Prop)
        {
            ThirdPersonCamera.SetActive(true);
            EmptyCamera.SetActive(true);
            FirstPersonCamera.SetActive(false);

            // Destroy the previous prop model if it exists
            if (propInstance != null)
            {
                NetworkServer.Destroy(propInstance);  // Properly destroy networked instance
            }

            // Randomly select a prop from the array of prop prefabs
            int randomIndex = Random.Range(0, propPrefabs.Length);

            // Instantiate the selected prop on the server
            propInstance = Instantiate(propPrefabs[randomIndex], transform.position, Quaternion.Euler(-90, 0, -90), transform);

            // Spawn the prop for all clients
            NetworkServer.Spawn(propInstance);
            Debug.Log($"Instantiated and spawned Prop model: {propPrefabs[randomIndex].name}");
        }
        else if (newRole == PlayerRole.Hunter)
        {
            ThirdPersonCamera.SetActive(false);
            EmptyCamera.SetActive(false);
            FirstPersonCamera.SetActive(true);

            if (propInstance != null)
            {
                NetworkServer.Destroy(propInstance);
                Debug.Log("Destroyed Prop model.");
            }
        }
    }


    //Network manager
    private CustomNetworkManager manager;

    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.Ready = newValue;
        }
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this.Ready, !this.Ready);
    }

    public void ChangeReady()
    {
        if (isOwned)
        {
            CmdSetPlayerReady();
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName());
        gameObject.name = "LocalGamePlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();      

    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager.GamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string PlayerName)
    {
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string OldValue, string NewValue)
    {
        if (isServer) //Host
        {
            this.PlayerName = NewValue;
        }
        if (isClient) //Client
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    // Start game
    public void CanStartGame(string SceneName)
    {
        if (isOwned)
        {
            CmdCanStartGame(SceneName);
        }
    }

    public void CmdCanStartGame(string SceneName)
    {
        manager.StartGame(SceneName);
    }

    private void Update()
    {
    
    }

    public void AssignRole(PlayerRole newRole)
    {
        if (isServer)
        {
            Debug.Log($"Assigning role: {newRole} to {gameObject.name}");
            PlayerRole oldRole = Role; // Store the old role
            Role = newRole;

            // Call OnRoleChanged to handle the transition
            RpcUpdateRole(newRole);
        }
        else
        {
            CmdSetRole(newRole);
        }
    }
}
