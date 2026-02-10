using System;

namespace CrocoType.Domain
{
    public class ToothManager
    {
        // runtime
        private int  _lethalIndex  = -1;
        private int  _toothCount   =  0;

        // injected
        private readonly Random _random;

        public ToothManager(Random random = null)
        {
            _random = random ?? new Random(); 
        }

        // public API
        public int LethalToothIndex => _lethalIndex;

        public void GenerateLethalTooth(int toothCount)
        {
            if (toothCount < 1)
                throw new ArgumentOutOfRangeException(nameof(toothCount), "Must be >= 1.");

            _toothCount   = toothCount;
            _lethalIndex  = _random.Next(0, toothCount); // [0, toothCount)
        }

        public bool IsLethal(int toothIndex) => toothIndex == _lethalIndex;
    }
}