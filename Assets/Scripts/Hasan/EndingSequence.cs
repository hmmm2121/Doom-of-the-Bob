namespace Official
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Video;
    using UnityEngine.SceneManagement;

    public class EndingSequence : MonoBehaviour
    {
        [Header("Boss")]
        public DoodleBobAI doodleBob;
        public Vector3 deathPosition = new Vector3(-1093f, -8.8f, -351f);
        public float arriveThreshold = 1.5f;

        [Header("Interaction")]
        public float interactRange = 8f;
        public KeyCode interactKey = KeyCode.E;
        public GameObject promptUI;

        [Header("Videos")]
        public VideoClip cutsceneClip;
        public VideoClip creditsClip;
        public string mainMenuScene = "MainMenu";

        [Header("Disable During Playback")]
        public MonoBehaviour[] scriptsToDisable;
        public GameObject[] objectsToHide;

        private Transform player;
        private bool playerInRange;
        private bool sequenceStarted;

        private Canvas videoCanvas;
        private RawImage videoImage;
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;

        void Start()
        {
            if (doodleBob == null) doodleBob = FindFirstObjectByType<DoodleBobAI>();

            var movement = FindFirstObjectByType<PlayerMovement>();
            if (movement != null)
            {
                player = movement.transform;
            }
            else
            {
                foreach (var t in GameObject.FindGameObjectsWithTag("Player"))
                {
                    if (t.GetComponent<Rigidbody>() != null) { player = t.transform; break; }
                }
            }

            if (promptUI != null) promptUI.SetActive(false);
        }

        private bool BossReady
        {
            get
            {
                if (doodleBob == null || !doodleBob.IsDead) return false;
                return Vector3.Distance(doodleBob.transform.position, deathPosition) <= arriveThreshold;
            }
        }

        void Update()
        {
            if (sequenceStarted || player == null) return;

            if (!BossReady)
            {
                if (playerInRange) { playerInRange = false; if (promptUI != null) promptUI.SetActive(false); }
                return;
            }

            float distance = Vector3.Distance(doodleBob.transform.position, player.position);
            if (distance <= interactRange)
            {
                if (!playerInRange)
                {
                    playerInRange = true;
                    if (promptUI != null) promptUI.SetActive(true);
                }

                if (Input.GetKeyDown(interactKey))
                    StartCoroutine(PlaySequence());
            }
            else if (playerInRange)
            {
                playerInRange = false;
                if (promptUI != null) promptUI.SetActive(false);
            }
        }

private IEnumerator PlaySequence()
        {
            sequenceStarted = true;

            // Kill level music so it doesn't play over the cutscene/credits video audio.
            MusicManager.StopMusic();

            if (RunTimer.Instance != null) RunTimer.Instance.FinishRun();

            if (promptUI != null) promptUI.SetActive(false);

            DisablePlayer();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            Time.timeScale = 1f;

            BuildOverlay();

            yield return PlayClip(cutsceneClip);
            yield return PlayClip(creditsClip);

            // Tear down the full-screen overlay, else (DontDestroyOnLoad) it survives the
            // scene load and hides the main menu behind a black canvas.
            if (videoPlayer != null) videoPlayer.Stop();
            if (renderTexture != null) renderTexture.Release();
            if (videoCanvas != null) Destroy(videoCanvas.gameObject);

            SceneManager.LoadScene(mainMenuScene);
        }

private void DisablePlayer()
        {
            if (scriptsToDisable != null)
                foreach (var s in scriptsToDisable)
                    if (s != null) s.enabled = false;
            if (objectsToHide != null)
                foreach (var go in objectsToHide)
                    if (go != null) go.SetActive(false);

            var move = FindFirstObjectByType<PlayerMovement>();
            if (move != null) move.enabled = false;

            var wall = FindFirstObjectByType<WallRide>();
            if (wall != null) wall.enabled = false;

            var look = FindFirstObjectByType<PlayerCamera>();
            if (look != null) look.enabled = false;

            var weapon = FindFirstObjectByType<Sponge.Weapon>();
            if (weapon != null)
            {
                weapon.enabled = false;
                weapon.gameObject.SetActive(false);
            }

            var hud = GameObject.Find("HUD_Canvas");
            if (hud != null) hud.SetActive(false);
        }


        private void BuildOverlay()
        {
            GameObject canvasGO = new GameObject("EndingVideoCanvas");
            videoCanvas = canvasGO.AddComponent<Canvas>();
            videoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            videoCanvas.sortingOrder = 1000;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject bgGO = new GameObject("Black");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = Color.black;
            StretchFull(bg.rectTransform);

            GameObject imgGO = new GameObject("VideoImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            videoImage = imgGO.AddComponent<RawImage>();
            StretchFull(videoImage.rectTransform);

            int w = Mathf.Max(640, Screen.width);
            int h = Mathf.Max(360, Screen.height);
            renderTexture = new RenderTexture(w, h, 0);
            videoImage.texture = renderTexture;

            videoPlayer = canvasGO.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;

            DontDestroyOnLoad(canvasGO);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private IEnumerator PlayClip(VideoClip clip)
        {
            if (clip == null) yield break;

            bool finished = false;
            VideoPlayer.EventHandler handler = vp => finished = true;

            videoPlayer.clip = clip;
            videoPlayer.loopPointReached += handler;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;

            videoPlayer.Play();
            while (!finished) yield return null;

            videoPlayer.loopPointReached -= handler;
            videoPlayer.Stop();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(deathPosition, interactRange);
        }
    }
}
