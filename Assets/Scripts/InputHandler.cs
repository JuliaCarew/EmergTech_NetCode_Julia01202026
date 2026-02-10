using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using CrocoType.Networking;

public class InputHandler : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] TextMeshProUGUI resultText;

    public SentenceGenerator stnGenerator;
    private GameSyncManager _syncManager;
    private bool _hasSubmitted = false;

    private void Start()
    {
        _syncManager = FindObjectOfType<GameSyncManager>();
        if (_syncManager == null)
        {
            Debug.LogError("InputHandler: GameSyncManager not found!");
        }
        else
        {
            // Subscribe to round start to reset submission state
            _syncManager.OnRoundStart += OnRoundStart;
            _syncManager.OnPhaseChanged += OnPhaseChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (_syncManager != null)
        {
            _syncManager.OnRoundStart -= OnRoundStart;
            _syncManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }
    
    private void OnRoundStart(string sentence, int roundNumber)
    {
        ResetSubmission();
    }
    
    private void OnPhaseChanged(CrocoType.Networking.GamePhase phase)
    {
        // Reset when entering typing phase
        if (phase == CrocoType.Networking.GamePhase.Typing)
        {
            ResetSubmission();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !_hasSubmitted)
        {
            ValidateInput();
        }
    }
    
    public void ValidateInput()
    {
        if (_hasSubmitted)
            return; // Prevent multiple submissions

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
            
            // Send to server immediately when correct
            if (_syncManager != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                float timestamp = Time.time;
                _syncManager.SubmitTypingInputServerRpc(input, timestamp);
                _hasSubmitted = true;
                inputField.interactable = false; // Disable input after submission
                Debug.Log($"InputHandler: Sent correct input to server (timestamp: {timestamp})");
            }
        }
    }
    
    // Reset submission flag when a new round starts
    public void ResetSubmission()
    {
        _hasSubmitted = false;
        if (inputField != null)
        {
            inputField.interactable = true;
            inputField.text = "";
        }
    }
}