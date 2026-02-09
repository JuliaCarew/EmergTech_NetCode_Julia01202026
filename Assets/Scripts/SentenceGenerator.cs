using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// Custom serializable wrapper for strings in NetworkVariables
public struct NetworkString : INetworkSerializable
{
    public string Value;

    public NetworkString(string value)
    {
        Value = value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }

    public static implicit operator string(NetworkString networkString) => networkString.Value;
    public static implicit operator NetworkString(string value) => new NetworkString(value);
}

public class SentenceGenerator : NetworkBehaviour
{
    #region sentences
    private List<string> sentences = new List<string>()
    {
        "The quick brown fox jumps over the lazy dog",
        "She couldn't decide of the glass was half empty or half full so she drank it",
        "Please put on these earmuffs because I can't you hear",
        "He said he was not there yesterday; however, many people saw him there",
        "It would have been a better night if the guys next to us weren't in the splash zone",
        "The hawk didn�t understand why the ground squirrels didn�t want to be his friend",
        "The tattered work gloves speak of the many hours of hard labor he endured throughout his life",
        "I love bacon, beer, birds, and baboons",
        "It isn't true that my mattress is made of cotton candy",
        "The hand sanitizer was actually clear glue",
        "The manager of the fruit stand always sat and only sold vegetables",
        "He used to get confused between soldiers and shoulders, but as a military man, he now soldiers responsibility",
        "She could hear him in the shower singing with a joy she hoped he'd retain after she delivered the news",
        "It was always dangerous to drive with him since he insisted the safety cones were a slalom course",
        "As he waited for the shower to warm, he noticed that he could hear water change temperature",
        "You have no right to call yourself creative until you look at a trowel and think that it would make a great lockpick",
        "Patricia loves the sound of nails strongly pressed against the chalkboard",
        "Jenny made the announcement that her baby was an alien",
        "The thick foliage and intertwined vines made the hike nearly impossible",
        "At that moment I was the most fearsome weasel in the entire swamp"
    };
    #endregion

    public string currentSentence { get; private set; }

    public NetworkVariable<NetworkString> Sentence = new NetworkVariable<NetworkString>(
        new NetworkString(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public TextMeshProUGUI sentenceText;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to NetworkVariable changes so all clients update when sentence changes
        Sentence.OnValueChanged += OnSentenceChanged;
        
        // Update UI with current NetworkVariable value if it exists
        if (Sentence.Value.Value != "")
        {
            OnSentenceChanged(new NetworkString(""), Sentence.Value);
        }
        
        Debug.Log($"SentenceGenerator OnNetworkSpawn - IsServer: {IsServer}, Sentence.Value: '{Sentence.Value.Value}'");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Sentence.OnValueChanged -= OnSentenceChanged;
    }

    private void OnSentenceChanged(NetworkString previousValue, NetworkString newValue)
    {
        currentSentence = newValue.Value;
        Debug.Log($"OnSentenceChanged called - Previous: '{previousValue.Value}', New: '{newValue.Value}'");
        
        if (sentenceText != null)
        {
            UpdateSentenceUI(newValue.Value);
            Debug.Log($"UI updated with sentence: '{newValue.Value}'");
        }
        else
        {
            Debug.LogError("sentenceText is null! Make sure it's assigned in the Inspector.");
        }
    }

    public void UpdateSentence()
    {
        // Only server can update the sentence
        if (!IsServer)
        {
            Debug.LogWarning("Only server can update the sentence!");
            return;
        }

        // Pick a new sentence and set it on the NetworkVariable
        string newSentence = PickSentence();
        Sentence.Value = new NetworkString(newSentence);
        
        Debug.Log($"Server set sentence to: '{newSentence}'");
        
        // Also update UI immediately on server (clients will get it via OnValueChanged)
        if (sentenceText != null)
        {
            UpdateSentenceUI(newSentence);
        }
    }

    // ________________________________________
    string PickSentence()
    {
        string randomSentence = sentences[Random.Range(0, sentences.Count)];
        return randomSentence;
    }    

    void UpdateSentenceUI(string sentence)
    {
        if (sentenceText == null)
        {
            Debug.LogError("sentenceText is null! Cannot update UI.");
            return;
        }
        
        sentenceText.text = sentence;
        Debug.Log($"UpdateSentenceUI called with: '{sentence}'");
    }
}
