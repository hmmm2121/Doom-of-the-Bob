using UnityEngine;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine.UI;

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
        foreach(Resolution res in AllResolutions)
        {
            newRes = res.width.ToString() + " x " + res.height.ToString();

            if (!resolutionStringList.Contains(newRes))
            {
                resolutionStringList.Add(newRes);
                SelectedResolutionList.Add(res);
            }
        }
        ResDropDown.AddOptions(resolutionStringList);
    }

    public void ChangeResolution()
    {
        SelectedResolution = ResDropDown.value;
        Screen.SetResolution(SelectedResolutionList[SelectedResolution].width, SelectedResolutionList[SelectedResolution].height, isFullScreen);
    }
}
