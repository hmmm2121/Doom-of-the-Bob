using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections;

public class SoundSetting : MonoBehaviour
{
    [SerializeField] Slider soundSlider;
    [SerializeField] AudioMixer masterMixer;
    [SerializeField] float defaultVolume = 100f;

    private void Start()
    {
        soundSlider.minValue = 0;
        soundSlider.maxValue = 100;
        soundSlider.wholeNumbers = false;

        StartCoroutine(ApplySavedVolume());
    }

    private IEnumerator ApplySavedVolume()
    {
        yield return null;
        SetVolume(PlayerPrefs.GetFloat("SavedMasterVolume", defaultVolume));
    }

    public void SetVolume(float _value)
    {
        if (_value < 0.001f) _value = 0.001f;

        Debug.Log("SetVolume called with: " + _value);

        RefreshSlider(_value);
        PlayerPrefs.SetFloat("SavedMasterVolume", _value);
        masterMixer.SetFloat("MasterVolume", Mathf.Log10(_value / 100f) * 20f);
    }

    public void SetVolumeFromSlider()
    {
        SetVolume(soundSlider.value);
    }

    public void RefreshSlider(float _value)
    {
        soundSlider.SetValueWithoutNotify(_value);
    }
}