using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AmmoUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI magAmmoText;
    public TextMeshProUGUI reserveAmmoText;

    [Header("Shells")]
    public Image[] shellImages;

    [Header("Panel")]
    public Image panelBackground;

    [Header("Color Settings")]
    public Color normalTextColor = new Color(0.23f, 0.12f, 0f);
    public Color lowTextColor = new Color(1f, 0f, 0f);
    public Color normalShellColor = new Color(0.78f, 0.52f, 0.04f);
    public Color emptyShellColor = new Color(0.78f, 0.52f, 0.04f, 0.2f);
    public Color lowShellColor = new Color(0.91f, 0.19f, 0.19f);

    private Weapon weapon;

    void Start()
    {
        weapon = FindObjectOfType<Weapon>();
    }

    void Update()
    {
        if (weapon == null) return;

        int mag = weapon.currentMagAmmo;
        int reserve = weapon.currentReserveAmmo;

        magAmmoText.text = mag.ToString();
        reserveAmmoText.text = "| " + reserve.ToString();

        // calculate how empty the mag is where 0 means the mag is full and 1 means the mag is empty
        float emptyRatio = 0f;
        if (weapon.maxMagAmmo > 0)
            emptyRatio = 1f - ((float)mag / (float)weapon.maxMagAmmo);

        // gradually go from normal color to red as ammo decreases
        Color textColor = Color.Lerp(normalTextColor, lowTextColor, emptyRatio);
        magAmmoText.color = textColor;
        reserveAmmoText.color = textColor;

        // Update shells
        for (int i = 0; i < shellImages.Length; i++)
        {
            if (i < mag)
                shellImages[i].color = Color.Lerp(normalShellColor, lowShellColor, emptyRatio);
            else
                shellImages[i].color = emptyShellColor;
        }
    }
}