using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea] public string text;
    public bool leftSpeaking;
}

public class DialogueManager : MonoBehaviour
{
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public Image Spongebob;
    public Image Patrick;

    public List<DialogueLine> lines = new List<DialogueLine>();

    public float typeSpeed = 0.03f;
    private int currentLine = 0;
    private bool isTyping = false;

    private Coroutine typingRoutine;

    public GameObject uiCanvas;

    void Start()
    {
        Time.timeScale = 0f;
        uiCanvas.SetActive(false);
        ShowLine();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
                FinishTyping();
            else
                Advance();
        }
    }

    void Advance()
    {
        currentLine++;
        if (currentLine < lines.Count)
            ShowLine();
        else
            EndDialogue();
    }

    void ShowLine()
    {
        DialogueLine line = lines[currentLine];
        speakerText.text = line.speakerName;

        Color active = Color.white;
        Color dimmed = new Color(0.5f, 0.5f, 0.5f, 1f);

        Spongebob.color = line.leftSpeaking ? active : dimmed;
        Patrick.color = line.leftSpeaking ? dimmed : active;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = StartCoroutine(TypeText(line.text));
    }

    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        isTyping = false;
    }

    void FinishTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        dialogueText.text = lines[currentLine].text;
        isTyping = false;
    }

    void EndDialogue()
    {
        Debug.Log("Dialogue finished");
        Time.timeScale = 1f;
        uiCanvas.SetActive(true);
        gameObject.SetActive(false);
    }
}