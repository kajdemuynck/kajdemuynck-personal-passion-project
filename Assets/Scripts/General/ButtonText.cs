using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    private Button btn;
    private TMP_Text txt;

    private void Awake()
    {
        btn = gameObject.GetComponent<Button>();
        txt = btn.GetComponentInChildren<TMP_Text>();
    }

    public void OnEnable()
    {
        txt.color = new Color(0.2f, 0.2f, 0.2f, 1);
    }

    public void OnDisable()
    {
        txt.color = new Color(0.2f, 0.2f, 0.2f, 1);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        txt.color = new Color(0, 0, 0, 1);
    }

    public void OnSelect(BaseEventData eventData)
    {
        txt.color = new Color(1, 1, 1, 1);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        txt.color = new Color(0.2f, 0.2f, 0.2f, 1);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        txt.color = new Color(1, 1, 1, 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        txt.color = new Color(0.2f, 0.2f, 0.2f, 1);
    }
}
