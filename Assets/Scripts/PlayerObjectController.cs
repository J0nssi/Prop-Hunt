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

    private bool isPropFrozen = false;
    private Vector3 frozenPosition; // Store the frozen position
    private Quaternion frozenRotation;

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
            RpcUpdateRole(Role);
        }
    }

    [ClientRpc]
    void RpcUpdateRole(PlayerRole newRole)
    {
        Debug.Log($"RpcUpdateRole: Updating role to {newRole} on client for {gameObject.name}");
        Role = newRole;

        // Handle any client-side changes after the role update
        if (isLocalPlayer)
        {
            Debug.Log($"RpcUpdateRole: Role updated for local player {gameObject.name}, new role: {newRole}");
            HandleRoleChange(newRole);
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
                if (Input.GetKeyDown(KeyCode.R))
                {
                    CmdToggleFreezeProp();    // Request the server to freeze the prop
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
            
            GameObject chosenProp = null;
            int randomIndex;
            Debug.Log($"Current prop: {currentProp?.name}");

            // Make sure to choose a prop that is different from the current one
            do
            {
                // Randomly select a prop model
                randomIndex = Random.Range(0, PropModels.Length);
                chosenProp = PropModels[randomIndex];
                Debug.Log($"Trying to choose prop: {chosenProp.name}");
            } while (currentProp != null && chosenProp == currentPropPrefab);

            // Step 3: Instantiate the chosen prop model on the server
            GameObject propInstance = Instantiate(chosenProp, transform.position, Quaternion.identity);
            Debug.Log($"Prop Position: {propInstance.transform.position}");
            // Attach the prop to the player by making it a child of the player
            AttachAndPositionProp(propInstance);

            // Step 4: Spawn the prop on the network and inform all clients
            NetworkServer.Spawn(propInstance, connectionToClient);
            if (currentProp != null)
            {
                Debug.Log($"Destroying current prop: {currentProp.name}");
                NetworkServer.Destroy(currentProp);
            }

            currentProp = propInstance;
            currentPropPrefab = chosenProp;
            // Use ClientRpc to sync the prop instantiation across all clients
            RpcAssignPropOnClient(propInstance);
        }
    }

    [ClientRpc]
    private void RpcAssignPropOnClient(GameObject propInstance)
    {
        if (!isServer)
        {
            AttachAndPositionProp(propInstance);
            propInstance.transform.position = transform.position;
            propInstance.transform.rotation = transform.rotation;
            currentProp = propInstance;
            currentPropPrefab = propInstance;
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
            GameObject chosenHunter = HunterPrefab;
            Debug.Log("Assigning Hunter...");

            // Destroy current prop if it exists
            if (currentHunter != null)
            {
                NetworkServer.Destroy(currentHunter);
                Debug.Log($"Destroyed current Hunter: {currentHunter.name}");
            }

            // Instantiate the Hunter prefab
            GameObject hunterInstance = Instantiate(chosenHunter, transform.position, Quaternion.identity);

            // Spawn the Hunter prefab on the network
            NetworkServer.Spawn(hunterInstance, connectionToClient);

            // Update references
            currentHunter = hunterInstance; // Rename to currentHunter

            // Notify clients about the new Hunter object
            RpcAssignHunterOnClient(hunterInstance);
        }
    }

    [ClientRpc]
    private void RpcAssignHunterOnClient(GameObject hunterInstance)
    {
        if (!isServer)
        {
            Debug.Log("Updating client with new Hunter instance.");
            AttachAndPositionHunter(hunterInstance);

            // Set position and rotation
            hunterInstance.transform.position = transform.position;
            hunterInstance.transform.rotation = transform.rotation;

            currentHunter = hunterInstance;

            Debug.Log("Client has updated the Hunter instance.");
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
        ChangeProp();  // Run on server
    }

    [ClientRpc]
    private void RpcChangeProp()
    {
        Debug.Log("RpcChangeProp called.");
        ChangeProp();  // Also run on clients
    }

    private void ChangeProp()
    {
        AssignProps();
    }

    [Command]
    private void CmdToggleFreezeProp()
    {
        isPropFrozen = !isPropFrozen; // Toggle the frozen state
        RpcToggleFreezeProp(isPropFrozen);
    }

    [ClientRpc]
    private void RpcToggleFreezeProp(bool frozen)
    {
        if (frozen)
        {
            frozenPosition = transform.position; // Store the current position
            frozenRotation = transform.rotation; // Store the current rotation
        }
        else
        {
            // Optionally handle any logic for when the prop is unfrozen
        }

        // Update the frozen state
        isPropFrozen = frozen;
    }

    private void FixedUpdate()
    {
        if (Role == PlayerRole.Prop && isPropFrozen)
        {
            // Reset the position and rotation to the frozen state
            transform.position = frozenPosition;
            transform.rotation = frozenRotation;
        }
    }
}
