using UnityEngine;

namespace Foldspace.Feedback
{
    /// <summary>Round-robin one-shot player for the <see cref="SoundLibrary"/>.</summary>
    public class AudioManager : MonoBehaviour
    {
        static readonly int[] ChainSemitones = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };

        [SerializeField] SoundLibrary library;
        [Range(0f, 1f)] [SerializeField] float volume = 0.5f;
        [SerializeField] int voiceCount = 12;

        AudioSource[] voices;
        int nextVoice;

        public void Init()
        {
            if (voices != null) return;
            voices = new AudioSource[Mathf.Max(1, voiceCount)];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
        }

        public void Play(SfxId id, float pitch = 1f, float gain = 1f)
        {
            if (voices == null || library == null) return;
            var entry = library.Get(id);
            if (entry == null) return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = pitch;
            voice.PlayOneShot(entry.clip, gain * entry.volume * volume);
        }

        /// <summary>Each step of a fold chain plays one note higher up a major pentatonic scale.</summary>
        public void PlayFold(int chain)
        {
            int step = Mathf.Clamp(chain - 1, 0, ChainSemitones.Length - 1);
            Play(SfxId.Fold, Mathf.Pow(2f, ChainSemitones[step] / 12f));
        }
    }
}
