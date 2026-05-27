using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResolutionControl : MonoBehaviour
{
    public TMP_Dropdown ResDropDown;
    bool isFullScreen;
    Resolution[] AllResolutions;
    int SelectedResolution;
    List<Resolution> SelectedResolutionList = new List<Resolution>();

    private void Start()
    {
        isFullScreen = true;
        AllResolutions = Screen.resolutions;

        List<string> resolutionStringList = new List<string>();
        string newRes;
        foreach (Resolution res in AllResolutions)
        {
            newRes = res.width.ToString() + " x " + res.height.ToString();
            if (!resolutionStringList.Contains(newRes))
            {
                resolutionStringList.Add(newRes);
                SelectedResolutionList.Add(res);
            }
        }
        ResDropDown.ClearOptions();
        ResDropDown.AddOptions(resolutionStringList);

        for (int i = 0; i < SelectedResolutionList.Count; i++)
        {
            if (SelectedResolutionList[i].width == Screen.width &&
                SelectedResolutionList[i].height == Screen.height)
            {
                ResDropDown.value = i;
                break;
            }
        }
        ResDropDown.RefreshShownValue();
    }

    public void ChangeResolution()
    {
        SelectedResolution = ResDropDown.value;
        Resolution r = SelectedResolutionList[SelectedResolution];
        Debug.Log("Setting resolution to " + r.width + " x " + r.height);
        Screen.SetResolution(r.width, r.height, isFullScreen);
    }
}