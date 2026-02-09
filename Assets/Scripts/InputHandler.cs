using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class InputHandler : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI resultText;

    public SentenceGenerator stnGenerator;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ValidateInput();
        }
    }
    public void ValidateInput()
    {
        string input = inputField.text;
        // Use the NetworkVariable value so all clients check against the same synchronized sentence
        string checkSentence = stnGenerator.Sentence.Value.Value;

        // check if input is exact string from sentence generator
        if (input != checkSentence) 
        {
            resultText.text = "Wrong";
            resultText.color = Color.red;
            Debug.Log("Wrong input");
        }
        else
        {
            resultText.text = "Correct!";
            resultText.color = Color.green;
            Debug.Log("Right input");
        }

        // need to check if the player's string sentence contains a wrong character
        if (!checkSentence.Contains(input))
        {
            UpdateWrongCharacters(input);
        }
    }

    private void UpdateWrongCharacters(string wrongChar)
    {
        // after player submits guess, set highlight color to red
        //wrongChar.text.color = Color.cyan;
        //inputField.text.color = Color.red;
        Debug.Log("INPUT: updating wrong characters");
    }
}