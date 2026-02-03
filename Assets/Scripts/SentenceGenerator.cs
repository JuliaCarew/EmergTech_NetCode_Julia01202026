using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

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
        "The hawk didn’t understand why the ground squirrels didn’t want to be his friend",
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

    public NetworkVariable<string> Sentence = new NetworkVariable<string>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    public TextMeshProUGUI sentenceText;

    private void Start()
    {
        //UpdateSentence();
    }

    //[ServerRpc]
    //private void SetSentenceServerRpc(string newSentence)
    //{
    //    Sentence.Value = newSentence;
    //    Debug.Log($"{Sentence.Value} Sentence set to: {newSentence}");
    //}

    public void UpdateSentence()
    {
        // update current sentecne w new one
        currentSentence = PickSentence();
        //SetSentenceServerRpc(currentSentence);

        Debug.Log($"currentSentence: {currentSentence}");

        UpdateSentenceUI(currentSentence);
        
    }

    // ________________________________________
    string PickSentence()
    {
        string randomSentence = sentences[Random.Range(0, sentences.Count)];
        return randomSentence;
    }

    void UpdateSentenceUI(string sentence)
    {
        sentenceText.text = sentence;
    }
}
