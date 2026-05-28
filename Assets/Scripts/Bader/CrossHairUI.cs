namespace Official
{
    using UnityEngine;
    using UnityEngine.UI;

    public class CrosshairUI : MonoBehaviour
    {
        [Header("Crosshair Lines (assign in Inspector)")]
        public RectTransform lineTop;
        public RectTransform lineBottom;
        public RectTransform lineLeft;
        public RectTransform lineRight;

        [Header("Crosshair Settings")]
        public float baseSpread = 20f;      // gap at rest
        public float maxSpread = 80f;       // gap at full recoil
        public float spreadLerpSpeed = 8f;  // how fast it snaps back

        private float currentSpread;
        private Weapon weapon;

        void Start()
        {
            weapon = FindObjectOfType<Weapon>();
            currentSpread = baseSpread;
        }

        void Update()
        {
            // Map weapon's recoil to crosshair spread
            float recoilAmount = 0f;
            if (weapon != null && weapon.weaponModel != null)
            {
                // How far back the weapon has kicked from its rest position
                float zDelta = weapon.GetRecoilOffset();
                recoilAmount = Mathf.Clamp01(zDelta / weapon.recoilBackAmount);
            }

            float targetSpread = Mathf.Lerp(baseSpread, maxSpread, recoilAmount);
            currentSpread = Mathf.Lerp(currentSpread, targetSpread, Time.deltaTime * spreadLerpSpeed);

            // Push each line outward by currentSpread
            lineTop.anchoredPosition = new Vector2(0, currentSpread);
            lineBottom.anchoredPosition = new Vector2(0, -currentSpread);
            lineLeft.anchoredPosition = new Vector2(-currentSpread, 0);
            lineRight.anchoredPosition = new Vector2(currentSpread, 0);
        }

        // Call this from Weapon.cs after firing to spike the spread
        public void TriggerSpread()
        {
            currentSpread = maxSpread;
        }
    }
}
