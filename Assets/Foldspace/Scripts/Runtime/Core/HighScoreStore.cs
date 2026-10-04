using UnityEngine;

namespace Foldspace
{
    /// <summary>Best score, persisted in PlayerPrefs.</summary>
    public sealed class HighScoreStore
    {
        const string Key = "foldspace.bestScore";

        public int Best { get; private set; }

        public HighScoreStore() => Best = PlayerPrefs.GetInt(Key, 0);

        /// <summary>Saves <paramref name="score"/> if it beats the best. Returns true for a new best.</summary>
        public bool Submit(int score)
        {
            if (score <= Best) return false;
            Best = score;
            PlayerPrefs.SetInt(Key, score);
            PlayerPrefs.Save();
            return true;
        }
    }
}
