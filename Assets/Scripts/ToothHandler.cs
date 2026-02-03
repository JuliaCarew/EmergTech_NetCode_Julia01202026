using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ToothHandler : NetworkBehaviour
{
    // get all teeth
    // register when player presses in elimination stage ONLY
    // take tooth out of pool

    public Button[] teeth;

    void Start()
    {
        foreach(Button tooth in teeth)
        {
            tooth.onClick.AddListener(BtnPress);
        }
    }

    void BtnPress()
    {
        Debug.Log("Pressed Tooth button!");
    }
}
