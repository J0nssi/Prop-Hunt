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

    public enum PlayerRole
    {
        Hunter,
        Prop
    }

    [SyncVar(hook = nameof(OnRoleChanged))]
    public PlayerRole Role;

    [SyncVar(hook = nameof(OnColorChanged))]
    private Color playerColor;

    private Renderer playerRenderer;

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
        Transform cubeTransform = transform.Find("Player/Cube"); // Adjust the path to your child object
        if (cubeTransform != null)
        {
            playerRenderer = cubeTransform.GetComponent<Renderer>(); // Get the Renderer from the Cube
        }

        // Set initial appearance based on the role
        UpdatePlayerAppearanceBasedOnRole(Role);
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

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        // Update appearance based on the new role
        if (isClient)
        {
            UpdatePlayerAppearanceBasedOnRole(newRole);
        }
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = newColor;
        }
    }

    private void UpdatePlayerAppearanceBasedOnRole(PlayerRole role)
    {
        Color color;
        if (role == PlayerRole.Hunter)
        {
            color = Color.red;
        }
        else // PlayerRole.Prop
        {
            color = Color.blue;
        }

        // Update the SyncVar for color
        CmdUpdateColor(color);
    }

    // Command to update the color on the server
    [Command]
    private void CmdUpdateColor(Color color)
    {
        playerColor = color;
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
                HandlePropAbilities();
            }
        }
    }

    private void HandleHunterAbilities()
    {
        // Implement Hunter-specific abilities (e.g., tracking, attacking)
        // Color should already be handled by OnColorChanged
    }

    private void HandlePropAbilities()
    {
        // Implement Prop-specific abilities (e.g., hiding, transforming)
        // Color should already be handled by OnColorChanged
    }
}
