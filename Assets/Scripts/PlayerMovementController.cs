using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovemenController : NetworkBehaviour
{

    public float Speed = 0.1f;
    public GameObject PlayerModel;

    private void Start()
    {
        PlayerModel.SetActive(false);
    }

    private void Update()
    {
        if(SceneManager.GetActiveScene().name == "Game")
        {
            if (PlayerModel.activeSelf == false)
            {
                PlayerModel.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (isOwned)
            {
                Movement();
            }
            
        }
    }

    private void SetPosition()
    {
        Vector3 newPosition = new Vector3(Random.Range(-5, 5), 0.8f, Random.Range(-15, 7));
        Debug.Log($"Setting position to {newPosition}");
        transform.position = newPosition;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        SetPosition();
        PlayerModel.SetActive(true); // Ensure the player model is active
    }

    public void Movement()
    {
        float xDirection = Input.GetAxis("Horizontal");
        float zDirection = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(xDirection, 0.0f, zDirection);

        transform.position += moveDirection * Speed;
    }
}
