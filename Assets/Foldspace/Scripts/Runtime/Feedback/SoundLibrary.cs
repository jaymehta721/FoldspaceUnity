using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foldspace.Feedback
{
    public enum SfxId
    {
        Fold,
        FoldEmpty,
        Kill,
        Hit,
        Shard,
        Dash,
        Warn,
        Deflect,
        LevelUp,
        Death,
        Launch,
        Pop,
        Bump,
        Implode,
    }

    /// <summary>Maps each sound effect to a clip and a mix volume. Swap clips here to reskin the audio.</summary>
    [CreateAssetMenu(menuName = "Foldspace/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 2f)] public float volume = 1f;
        }

        public List<Entry> entries = new List<Entry>();

        Entry[] lookup;

        public Entry Get(SfxId id)
        {
            if (lookup == null) BuildLookup();
            int index = (int)id;
            return index < lookup.Length ? lookup[index] : null;
        }

        void OnValidate() => lookup = null;

        void BuildLookup()
        {
            lookup = new Entry[Enum.GetValues(typeof(SfxId)).Length];
            foreach (var entry in entries)
                if (entry != null && entry.clip != null) lookup[(int)entry.id] = entry;
        }
    }
}
