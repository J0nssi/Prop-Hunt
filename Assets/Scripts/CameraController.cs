using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class CameraController : NetworkBehaviour
{
    public GameObject cameraHolder;

    public override void OnStartAuthority()
    {
        cameraHolder.SetActive(true);
    }
    void Start()
    {
        // Disable the camera for all players initially, except for the local player
        if (!isOwned)
        {
            cameraHolder.SetActive(false); // Ensure the camera is off for non-local players.
        }
    }
}
