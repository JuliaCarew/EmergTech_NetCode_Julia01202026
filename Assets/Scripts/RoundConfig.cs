namespace CrocoType.Domain
{
    public sealed class RoundConfig
    {
        public int RoundNumber { get; }
        public string Sentence { get; }
        public int DifficultyTier { get; }
        public int ToothCount { get; }
        public float TypingTimeLimit { get; }
        public float ToothPickTimeLimit { get; }

        public RoundConfig(int    roundNumber,
                           string sentence,
                           int    difficultyTier,
                           int    toothCount,
                           float  typingTimeLimit    = 30f,
                           float  toothPickTimeLimit = 10f)
        {
            RoundNumber        = roundNumber;
            Sentence           = sentence;
            DifficultyTier     = difficultyTier;
            ToothCount         = toothCount;
            TypingTimeLimit    = typingTimeLimit;
            ToothPickTimeLimit = toothPickTimeLimit;
        }

        public override string ToString() =>
            $"Round {RoundNumber} | Tier {DifficultyTier} | Teeth {ToothCount} | \"{Sentence}\"";
    }
}
