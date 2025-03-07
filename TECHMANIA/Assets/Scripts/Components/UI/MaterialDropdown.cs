﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialDropdown : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler
{
    public List<Graphic> graphics;
    public Color enabledColor;
    public Color disabledColor;

    private bool interactable;

    // Refer to comments on MaterialTextField.frameOfEndEdit.
    private static bool anyDropdownExpanded;
    private static int frameOfLastCollapse;

    public static bool IsEditingAnyDropdown()
    {
        return anyDropdownExpanded ||
            frameOfLastCollapse == Time.frameCount;
    }

    private TMP_Dropdown dropdown;
    private bool expandedOnPreviousFrame;

    static MaterialDropdown()
    {
        anyDropdownExpanded = false;
        frameOfLastCollapse = -1;
    }

    // Start is called before the first frame update
    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        expandedOnPreviousFrame = false;
        interactable = true;
    }

    // Update is called once per frame
    void Update()
    {
        bool expanded = dropdown.IsExpanded;
        if (expandedOnPreviousFrame != expanded)
        {
            anyDropdownExpanded = expanded;
            if (expanded)
            {
                OnExpand();
            }
            else
            {
                OnCollapse();
            }
        }
        expandedOnPreviousFrame = expanded;

        bool newInteractable = dropdown.IsInteractable();
        if (newInteractable != interactable)
        {
            Color color = newInteractable ?
                enabledColor : disabledColor;
            graphics.ForEach(g => g.color = color);
        }
        interactable = newInteractable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!expandedOnPreviousFrame)
        {
            // PointerEnter covers all items when the dropdown
            // is expanded. We don't want that.
            if (eventData.pointerId < 0 && interactable)
            {
                MenuSfx.instance.PlaySelectSound();
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnExpand()
    {
        MenuSfx.instance.PlayClickSound();
    }

    public void OnCollapse()
    {
        MenuSfx.instance.PlayClickSound();
        frameOfLastCollapse = Time.frameCount;
    }
}
