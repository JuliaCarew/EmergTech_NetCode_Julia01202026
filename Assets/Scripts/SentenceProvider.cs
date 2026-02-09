using System;
using CrocoType.Interfaces;

namespace CrocoType.Providers
{
    public class SentenceProvider : ISentenceProvider
    {
        // sentences
        private static readonly string[] Sentences =
        {
            "the cat sat on the mat",
            "a dog and a bird play",
            "run fast and jump high",
            "she likes to read books",
            "we went to the park today",
            "the quick brown fox jumps over the lazy dog",
            "coding is fun when you enjoy the process",
            "the rain in spain falls mainly on the plain",
            "every cloud has a silver lining, they say",
            "practice makes perfect, so keep on typing",
            "the five boxing wizards jump quickly over the lazy dogs",
            "how vexingly quick daft zebras jump, as everyone knows",
            "pack my box with five dozen liquor jugs; it's quite bizarre",
            "the job requires extra pluck and zeal from every young wage earner",
            "sphinx of black quartz, judge my vow: it was truly magnificent",
            "amazingly, quick brown foxes do jump over every single lazy dog without hesitation or fear",
            "we promptly judged antique ivory buckles for the next prize, which was quite extraordinary indeed",
            "the five boxing wizards; who jump quickly, were thoroughly amazed by the deft and nimble zebras",
            "jinxed wizards fight for equanimity (a rare virtue), while the lazy dog snoozes by the quartz bench"
        };

        // injected
        private readonly Random _random;

        public SentenceProvider(Random random = null)
        {
            _random = random ?? new Random();
        }

        //ISentenceProvider
        public string GetSentence()
        {
            return Sentences[_random.Next(Sentences.Length)];
        }
    }
}
