using TMPro;
using UnityEngine;

public class PerkSelection : MonoBehaviour
{
    public PerkData perkOptionA;
    public PerkData perkOptionB;
    public TMP_Text perk1Txt, perk2Txt, desc1, desc2;

    void Start()
    {
        PerkManager.Instance.ResetPerk();

        perk1Txt.text = perkOptionA.perkName;
        perk2Txt.text = perkOptionB.perkName;
        desc1.text = perkOptionA.description;
        desc2.text = perkOptionB.description;
    }

    public void ChoosePerk1()
    {
        PerkManager.Instance.SelectPerk(perkOptionA);
    }

    public void ChoosePerk2()
    {
        PerkManager.Instance.SelectPerk(perkOptionB);
    }
}