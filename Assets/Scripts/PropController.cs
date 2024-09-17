using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropController : PlayerMovemenController
{
    private void Start()
    {
        Speed = 0.05f; // Props move slower or have limited movement
    }

    // Add prop-specific abilities like hiding or mimicking here
}
