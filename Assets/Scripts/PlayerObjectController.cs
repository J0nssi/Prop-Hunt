using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class PlayerObjectController : NetworkBehaviour
{
    //Player Data
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;

    //Props
    public GameObject[] PropModels;
    private GameObject currentProp;

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
        if (newRole == PlayerRole.Prop)
        {
            AssignProps();  // Only if the player is a Prop
        }
        else if (newRole == PlayerRole.Hunter)
        {
            Debug.Log("Assigned as Hunter.");
        }
    }

    [Command]
    public void CmdSetRole(PlayerRole newRole)
    {
        Role = newRole;
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
        currentProp = null;
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
    private void CMdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this.Ready, !this.Ready);
    }

    public void ChangeReady()
    {
        if (isOwned)
        {
            CMdSetPlayerReady();
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
        if (isLocalPlayer)
        {
            if (Role == PlayerRole.Hunter)
            {
                HandleHunterAbilities();
            }
            else if (Role == PlayerRole.Prop)
            {
                if (Input.GetKeyDown(KeyCode.F))
            {
                CmdChangeProp();  // Request the server to change the prop
            }
            }
        }
    }

    public void AssignRole(PlayerRole newRole)
    {
        if (isServer)
        {
            Role = newRole;
        }
        else
        {
            CmdSetRole(newRole);
        }
    }

    private void HandleHunterAbilities()
    {
        // Implement Hunter-specific abilities (e.g., tracking, attacking)
        // Color should already be handled by OnColorChanged
    }

    private void AssignProps()
    {
        if (isServer)
        {
            // Debug log to check if this method is being called
            Debug.Log("AssignProps called.");
            
            GameObject chosenProp = null;
            int randomIndex;

            // Make sure to choose a prop that is different from the current one
            do
            {
                // Randomly select a prop model
                randomIndex = Random.Range(0, PropModels.Length);
                chosenProp = PropModels[randomIndex];
                Debug.Log($"Trying to choose prop: {chosenProp.name}");
            } while (currentProp != null && chosenProp.name == currentProp.name);

            // Step 3: Instantiate the chosen prop model on the server
            GameObject propInstance = Instantiate(chosenProp, transform.position, Quaternion.identity);

            // Attach the prop to the player by making it a child of the player
            propInstance.transform.SetParent(transform);

            // Ensure the camera's follow target (if any) stays the same
            Transform followTarget = transform.Find("CameraHolder/FollowTarget");
            if (followTarget != null)
            {
                // Adjust prop position to match where the cubes were
                propInstance.transform.localPosition = followTarget.localPosition;
                propInstance.transform.localRotation = Quaternion.identity;
            }

            // Step 4: Spawn the prop on the network and inform all clients
            NetworkServer.Spawn(propInstance);
            currentProp = propInstance;
            // Use ClientRpc to sync the prop instantiation across all clients
            RpcAssignPropOnClient(propInstance);
        }
    }
    [ClientRpc]
    private void RpcAssignPropOnClient(GameObject propInstance)
    {
        if (!isServer)
        {
            // Attach the prop to the player by making it a child of the player
            propInstance.transform.SetParent(transform);

            // Ensure the camera's follow target (if any) stays the same
            Transform followTarget = transform.Find("CameraHolder/FollowTarget");
            if (followTarget != null)
            {
                propInstance.transform.localPosition = followTarget.localPosition;
                propInstance.transform.localRotation = Quaternion.identity;
            }
            currentProp = propInstance;
        }
    }
    [Command]
    private void CmdChangeProp()
    {
        Debug.Log("CmdChangeProp called.");
        ChangeProp();  // Run on server
        RpcChangeProp();  // Sync with all clients
    }

    [ClientRpc]
    private void RpcChangeProp()
    {
        Debug.Log("RpcChangeProp called.");
        ChangeProp();  // Also run on clients
    }

    private void ChangeProp()
    {
        Debug.Log("ChangeProp called.");
        if (currentProp != null)
        {
            // Ensure this logic runs on both the server and clients
            if (currentProp != null)
            {
                Debug.Log($"Destroying current prop: {currentProp.name}");
                Destroy(currentProp);
                NetworkServer.Destroy(currentProp);
                currentProp = null;
            }
            else
            {
                Debug.Log("No current prop to destroy.");
            }

            // Assign a new random prop from the list
            AssignProps();
        }
    }
}
