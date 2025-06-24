using UnityEngine;
using TMPro;
using ArabicSupport;

public class ArabicTextHelper : MonoBehaviour
{
    public TMP_Text textMesh;

    public void SetText(string text)
    {
        if (ContainsArabic(text))
        {
            text = ArabicFixer.Fix(text, showTashkeel: false, useHinduNumbers: false);
            textMesh.isRightToLeftText = true;
        }
        else
        {
            textMesh.isRightToLeftText = false;
        }

        textMesh.text = text;
    }

    private bool ContainsArabic(string text)
    {
        foreach (char c in text)
        {
            if ((c >= 0x0600 && c <= 0x06FF) || (c >= 0x0750 && c <= 0x077F))
                return true;
        }
        return false;
    }
}
