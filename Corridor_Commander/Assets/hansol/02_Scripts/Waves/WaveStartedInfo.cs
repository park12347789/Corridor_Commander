namespace CorridorCommander
{
    public readonly struct WaveStartedInfo
    {
        public WaveStartedInfo(int waveIndex, EnemyWaveDefinition wave, bool hasBoss, int bossCount)
        {
            WaveIndex = waveIndex;
            WaveNumber = waveIndex + 1;
            Wave = wave;
            HasBoss = hasBoss;
            BossCount = bossCount;
        }

        public int WaveIndex { get; }
        public int WaveNumber { get; }
        public EnemyWaveDefinition Wave { get; }
        public bool HasBoss { get; }
        public int BossCount { get; }
    }
}
