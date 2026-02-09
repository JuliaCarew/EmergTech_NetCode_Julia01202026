namespace CrocoType.Domain
{
    public static class TypingEvaluator
    {
        public static bool IsComplete(string typed, string target)
        {
            if (string.IsNullOrEmpty(target)) return false;
            return typed == target;
        }

        public static int CorrectPrefixLength(string typed, string target)
        {
            if (string.IsNullOrEmpty(typed) || string.IsNullOrEmpty(target))
                return 0;

            int limit = System.Math.Min(typed.Length, target.Length);
            int count = 0;

            for (int i = 0; i < limit; i++)
            {
                if (typed[i] == target[i])
                    count++;
                else
                    break; // first mismatch ends the correct prefix
            }

            return count;
        }

        public static float Accuracy(string typed, string target)
        {
            if (string.IsNullOrEmpty(typed)) return 1.0f;
            if (string.IsNullOrEmpty(target)) return 0.0f;

            int correct = 0;
            int total   = typed.Length;

            for (int i = 0; i < total; i++)
            {
                if (i < target.Length && typed[i] == target[i])
                    correct++;
            }

            return (float)correct / total;
        }
    }
}
