using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public RectTransform rect;

    void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        rect.anchoredPosition = Input.mousePosition / rect.transform.localScale.x;
    }
}