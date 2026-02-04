using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

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
    }

}
// wrong characters highlighted in red