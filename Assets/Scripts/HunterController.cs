using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterController : PlayerMovemenController
{
    private void Start()
    {
        Speed = 0.15f; // Hunters move faster than props
    }

    // Hunter-specific behaviors can go here, like detecting props
}
}
