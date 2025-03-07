﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shared by notes and markers, this component positions
// the GameObject at the appropriate position in the workspace.
public class SelfPositionerInEditor : MonoBehaviour
{
    private void OnEnable()
    {
        PatternPanel.RepositionNeeded += Reposition;
    }

    private void OnDisable()
    {
        PatternPanel.RepositionNeeded -= Reposition;
    }

    public void Reposition()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;

        float scan = 0f;
        Marker marker = GetComponent<Marker>();
        ScanlineInEditor scanline = GetComponent<ScanlineInEditor>();
        NoteObject noteObject = GetComponent<NoteObject>();
        if (marker != null)
        {
            float beat = (float)marker.pulse / Pattern.pulsesPerBeat;
            scan = beat / bps;
        }
        else if (scanline != null)
        {
            float beat = scanline.floatPulse / Pattern.pulsesPerBeat;
            scan = beat / bps;
        }
        else
        {
            float beat = (float)noteObject.note.pulse / 
                Pattern.pulsesPerBeat;
            scan = beat / bps;
        }
        float x = PatternPanel.ScanWidth * scan;

        float y = 0;
        if (marker != null)
        {
            // Don't change y.
            y = GetComponent<RectTransform>().anchoredPosition.y;
        }
        else if (scanline != null)
        {
            // y is 0.
        }
        else if (noteObject != null)
        {
            y = -PatternPanel.LaneHeight *
                (noteObject.note.lane + 0.5f);
        }

        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        if (noteObject != null)
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(
                PatternPanel.LaneHeight, PatternPanel.LaneHeight);
        }
    }
}
