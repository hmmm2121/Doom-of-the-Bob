namespace Official
{
    using UnityEngine;

    // Per-scene music controller. Lives on the scene's music AudioSource object
    // (e.g. "MainMenu Theme" in menus, "LevelMusic" in levels). NOT persistent —
    // each scene owns its own music, so menu music never bleeds into levels.
    // Exposes a static handle for pausing/stopping during pause menus and cutscenes.
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Current { get; private set; }

        private AudioSource audioSource;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            Current = this;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public static void PauseMusic()  { if (Current != null && Current.audioSource != null) Current.audioSource.Pause(); }
        public static void ResumeMusic() { if (Current != null && Current.audioSource != null) Current.audioSource.UnPause(); }
        public static void StopMusic()   { if (Current != null && Current.audioSource != null) Current.audioSource.Stop(); }
    }
}
