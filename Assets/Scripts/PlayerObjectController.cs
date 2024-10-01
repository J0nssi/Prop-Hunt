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

    //Props
    public GameObject[] PropModels;
    private GameObject currentProp;
    private GameObject currentPropPrefab;

    //Hunter
    public GameObject HunterPrefab;
    private GameObject currentHunter;


    //Roles
    public enum PlayerRole
    {
        Hunter,
        Prop
    }

    [SyncVar]
    public PlayerRole Role;




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

        // Instantiate the appropriate model based on the role
        if (newRole == PlayerRole.Hunter)
        {
            AssignHunter(); // Assign Hunter model
        }
        else if (newRole == PlayerRole.Prop)
        {
            AssignProps(); // Assign Prop model
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
        }
        else if (newRole == PlayerRole.Hunter)
        {
            ThirdPersonCamera.SetActive(false);
            EmptyCamera.SetActive(false);
            FirstPersonCamera.SetActive(true);
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
        currentProp = null;
        currentHunter = null;
        
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
  public void Quit()
    {
        //Set the offline scene to null
        manager.offlineScene = "";

        //Make the active scene the offline scene
        SceneManager.LoadScene("Menu");

        //Leave Steam Lobby
        SteamLobby.Instance.LeaveLobby();

        if (isOwned)
        {
            if (isServer)
            {
                manager.StopHost();
            }
            else
            {
                manager.StopClient();
            }
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


    private void HandleHunterAbilities()
    {
        // Implement Hunter-specific abilities (e.g., tracking, attacking)
        // Color should already be handled by OnColorChanged
    }


    private void AssignProps()
    {
        if (isServer)
        {
            GameObject chosenProp = PropModels[Random.Range(0, PropModels.Length)]; // Randomly select a prop

            // Instantiate and attach the prop to the player
            GameObject propInstance = Instantiate(chosenProp, transform.position, Quaternion.Euler(-90, 0, 90));
            AttachAndPositionProp(propInstance);

            // Spawn the prop on the network
            NetworkServer.Spawn(propInstance, connectionToClient);

            if (currentProp != null)
            {
                NetworkServer.Destroy(currentProp); // Destroy the old prop
            }

            currentProp = propInstance; // Update the reference
            RpcAssignPropOnClient(propInstance); // Inform all clients
        }
    }


    [ClientRpc]
    private void RpcAssignPropOnClient(GameObject propInstance)
    {
        if (!isServer)
        {
            AttachAndPositionProp(propInstance); // Position it for clients
            currentProp = propInstance;
        }
    }

    private void AttachAndPositionProp(GameObject propInstance)
    {
        // Attach the prop as a child of the player
        propInstance.transform.SetParent(transform);

        Transform followTarget = transform.Find("CameraHolder/FollowTarget");
        if (followTarget != null)
        {
            propInstance.transform.localPosition = followTarget.localPosition; // Position relative to the follow target
        }
        propInstance.transform.localRotation = Quaternion.Euler(-90, 0, -90);
    }

    private void AssignHunter()
    {
        if (isServer)
        {
            GameObject hunterInstance = Instantiate(HunterPrefab, transform.position, Quaternion.identity);
            AttachAndPositionHunter(hunterInstance);

            // Spawn the Hunter on the network
            NetworkServer.Spawn(hunterInstance, connectionToClient);

            if (currentHunter != null)
            {
                NetworkServer.Destroy(currentHunter); // Destroy the old hunter, if any
            }

            currentHunter = hunterInstance; // Update the reference
            RpcAssignHunterOnClient(hunterInstance); // Inform all clients
        }
    }

    [ClientRpc]
    private void RpcAssignHunterOnClient(GameObject hunterInstance)
    {
        if (!isServer)
        {
            AttachAndPositionHunter(hunterInstance); // Position it for clients
            currentHunter = hunterInstance;
        }
    }


    private void AttachAndPositionHunter(GameObject hunterInstance)
    {
        // Attach the Hunter prefab as a child of the player
        hunterInstance.transform.SetParent(transform);

        Transform followTarget = transform.Find("CameraHolder/FollowTarget");
        if (followTarget != null)
        {
            hunterInstance.transform.localPosition = followTarget.localPosition; // Position relative to the follow target
        }
        hunterInstance.transform.localRotation = Quaternion.identity;
    }

    [Command]
    private void CmdChangeProp()
    {
        if (isServer)
        {
            // Change to a new random prop on the server
            ChangeProp();
        }
        else
        {
            // Call RPC to update clients about the change
            RpcChangeProp();
        }
    }

    [ClientRpc]
    private void RpcChangeProp()
    {
        if (!isServer)
        {
            ChangeProp();  // Update the prop on all clients
        }
    }

    private void ChangeProp()
    {
        // Ensure we pick a different prop than the current one
        if (PropModels.Length == 0) return;

        GameObject chosenProp = null;
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, PropModels.Length);
            chosenProp = PropModels[randomIndex];
        }
        while (currentPropPrefab != null && chosenProp == currentPropPrefab);  // Ensure new prop is different

        if (chosenProp != null)
        {
            if (currentProp != null)
            {
                // Destroy the current prop if one exists
                NetworkServer.Destroy(currentProp);
            }

            // Instantiate the new prop
            GameObject newProp = Instantiate(chosenProp, transform.position, Quaternion.Euler(-90, 0, 90));

            // Attach the new prop to the player
            AttachAndPositionProp(newProp);

            // Spawn the new prop on the network and sync with clients
            NetworkServer.Spawn(newProp, connectionToClient);

            // Update references to the new prop
            currentProp = newProp;
            currentPropPrefab = chosenProp;

            // Inform all clients of the new prop
            RpcAssignPropOnClient(newProp);
        }
    }
}
