using TMPro;
using UnityEngine;

public class SubtitleWriter : MonoBehaviour
{
    [SerializeField] private string[] subtitleFills;
    private int currentSubtitle = 0;

    private TextMeshProUGUI textField;

    private void Awake()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    public void WriteNextSubtitle()
    {
        if (currentSubtitle >= subtitleFills.Length)
            return;

        WriteSubtitle(subtitleFills[currentSubtitle]);
        currentSubtitle++;
    }

    public void WriteSubtitle(string text)
    {
        textField.text = text;
    }
}
