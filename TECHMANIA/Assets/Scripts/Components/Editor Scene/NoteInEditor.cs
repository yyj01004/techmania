﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteInEditor : MonoBehaviour, IPointsOnCurveProvider
{
    public Sprite hiddenSprite;
    public Sprite hiddenTrailSprite;
    public Image selectionOverlay;
    public RectTransform endOfScanIndicator;
    public RectTransform noteImage;

    public RectTransform pathToPreviousNote;
    public RectTransform durationTrail;
    public RectTransform invisibleTrail;
    public Texture2D horizontalResizeCursor;
    public bool transparentNoteImageWhenTrailTooShort;

    [Header("Drag Note")]
    public Sprite hiddenCurveSprite;
    public CurvedImage curvedImage;
    public RectTransform anchorReceiverContainer;
    public GameObject anchorReceiverTemplate;
    public RectTransform anchorContainer;
    public GameObject anchorTemplate;
    public Texture2D addAnchorCursor;

    // GameObject in the following events all refer to
    // this.gameObject.

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;

    public static event UnityAction<PointerEventData, GameObject> 
        BeginDrag;
    public static event UnityAction<PointerEventData> Drag;
    public static event UnityAction<PointerEventData> EndDrag;

    public static event UnityAction<PointerEventData, GameObject> 
        DurationHandleBeginDrag;
    public static event UnityAction<PointerEventData> 
        DurationHandleDrag;
    public static event UnityAction<PointerEventData> 
        DurationHandleEndDrag;

    public static event UnityAction<PointerEventData, GameObject> 
        AnchorReceiverClicked;
    public static event UnityAction<PointerEventData> 
        AnchorClicked;
    public static event UnityAction<PointerEventData> 
        AnchorBeginDrag;
    public static event UnityAction<PointerEventData> AnchorDrag;
    public static event UnityAction<PointerEventData> AnchorEndDrag;

    // int is control point index (0 or 1).
    public static event UnityAction<PointerEventData, int> 
        ControlPointClicked;
    public static event UnityAction<PointerEventData, int> 
        ControlPointBeginDrag;
    public static event UnityAction<PointerEventData> ControlPointDrag;
    public static event UnityAction<PointerEventData> 
        ControlPointEndDrag;

    private void OnEnable()
    {
        PatternPanel.SelectionChanged += UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged += 
            UpdateKeysoundVisibility;
        resizeCursorState = 0;
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged -=
            UpdateKeysoundVisibility;
    }

    private Note GetNote()
    {
        return GetComponent<NoteObject>().note;
    }

    public void UseHiddenSprite()
    {
        noteImage.GetComponent<Image>().sprite = hiddenSprite;
        if (durationTrail != null)
        {
            durationTrail.GetComponent<Image>().sprite = 
                hiddenTrailSprite;
        }
        if (curvedImage != null)
        {
            curvedImage.sprite = hiddenCurveSprite;
        }
    }

    private void UpdateSelection(HashSet<GameObject> selection)
    {
        bool selected = selection.Contains(gameObject);
        if (selectionOverlay != null)
        {
            selectionOverlay.enabled = selected;
        }
        if (anchorReceiverContainer != null)
        {
            anchorReceiverContainer.gameObject.SetActive(selected);
        }
        if (anchorContainer != null)
        {
            anchorContainer.gameObject.SetActive(selected);
        }
    }

    public void UpdateKeysoundVisibility()
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .gameObject.SetActive(
            Options.instance.editorOptions.showKeysounds);
    }

    public void UpdateEndOfScanIndicator()
    {
        Note n = GetNote();
        bool showIndicator = n.endOfScan;
        if (showIndicator)
        {
            // Don't show indicator if the note is not on a
            // scan divider.
            int pulsesPerScan = Pattern.pulsesPerBeat *
                EditorContext.Pattern.patternMetadata.bps;
            if (n.pulse % pulsesPerScan != 0)
            {
                showIndicator = false;
            }

            // Don't show indicator on drag notes.
            if (n.type == NoteType.Drag)
            {
                showIndicator = false;
            }
        }

        if (showIndicator)
        {
            endOfScanIndicator.sizeDelta = new Vector2(
                endOfScanIndicator.sizeDelta.y,
                endOfScanIndicator.sizeDelta.y);
            endOfScanIndicator.GetComponent<Image>().enabled = true;
        }
        else
        {
            endOfScanIndicator.sizeDelta = new Vector2(
                0f,
                endOfScanIndicator.sizeDelta.y);
            endOfScanIndicator.GetComponent<Image>().enabled = false;
        }
    }

    public void SetKeysoundText()
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .text = UIUtils.StripAudioExtension(
                GetNote().sound);
    }

    private void Update()
    {

    }

    #region Event Relay From Note Image
    public void OnPointerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.dragging) return;

        switch (pointerData.button)
        {
            case PointerEventData.InputButton.Left:
                LeftClicked?.Invoke(gameObject);
                break;
            case PointerEventData.InputButton.Right:
                RightClicked?.Invoke(gameObject);
                break;
        }
    }

    public void OnBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        BeginDrag?.Invoke(eventData as PointerEventData, gameObject);
    }

    public void OnDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        Drag?.Invoke(eventData as PointerEventData);
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        EndDrag?.Invoke(eventData as PointerEventData);
    }
    #endregion

    #region Event Relay From Duration Handle
    private static int resizeCursorState;
    private void UseResizeCursor()
    {
        resizeCursorState++;
        if (resizeCursorState > 0 &&
            PatternPanel.tool == PatternPanel.Tool.Note)
        {
            UnityEngine.Cursor.SetCursor(horizontalResizeCursor,
                new Vector2(64f, 64f), CursorMode.Auto);
        }
    }

    private void UseDefaultCursor()
    {
        resizeCursorState--;
        if (resizeCursorState <= 0)
        {
            UIUtils.UseDefaultCursor();
        }
    }

    public void OnDurationHandlePointerEnter(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseResizeCursor();
    }

    public void OnDurationHandlePointerExit(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseDefaultCursor();
    }

    public void OnDurationHandleBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseResizeCursor();
        DurationHandleBeginDrag?.Invoke(
            eventData as PointerEventData, gameObject);
    }

    public void OnDurationHandleDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        DurationHandleDrag?.Invoke(pointerData);
    }

    public void OnDurationHandleEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseDefaultCursor();
        DurationHandleEndDrag?.Invoke(eventData as PointerEventData);
    }
    #endregion

    #region Event Relay From Curve
    public void OnAnchorReceiverPointerEnter()
    {
        if (PatternPanel.tool == PatternPanel.Tool.Note)
        {
            UnityEngine.Cursor.SetCursor(addAnchorCursor,
                Vector2.zero, CursorMode.Auto);
        }
    }

    public void OnAnchorReceiverPointerExit()
    {
        UIUtils.UseDefaultCursor();
    }

    public void OnAnchorReceiverClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        AnchorReceiverClicked?.Invoke(eventData as PointerEventData,
            gameObject);
    }

    public void OnAnchorClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        AnchorClicked?.Invoke(eventData as PointerEventData);
    }

    public void OnAnchorBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        AnchorBeginDrag?.Invoke(eventData as PointerEventData);
    }

    public void OnAnchorDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        AnchorDrag?.Invoke(eventData as PointerEventData);
    }

    public void OnAnchorEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        AnchorEndDrag?.Invoke(eventData as PointerEventData);
    }

    public void OnControlPointClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;

        GameObject clicked = pointerData.pointerPress;
        DragNoteAnchor anchor = clicked
            .GetComponentInParent<DragNoteAnchor>();
        int controlPointIndex = anchor
            .GetControlPointIndex(clicked);
        ControlPointClicked?.Invoke(
            pointerData, controlPointIndex);
    }

    public void OnControlPointBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;

        GameObject dragging = pointerData.pointerDrag;
        DragNoteAnchor anchor = dragging
            .GetComponentInParent<DragNoteAnchor>();
        int controlPointIndex = anchor
            .GetControlPointIndex(dragging);
        ControlPointBeginDrag?.Invoke(pointerData, controlPointIndex);
    }

    public void OnControlPointDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        ControlPointDrag?.Invoke(eventData as PointerEventData);
    }

    public void OnControlPointEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        ControlPointEndDrag?.Invoke(eventData as PointerEventData);
    }
    #endregion

    #region Note Image and Path
    public void ResetNoteImageRotation()
    {
        noteImage.localRotation = Quaternion.identity;
    }

    public void KeepPathInPlaceWhileNoteBeingDragged(
        Vector2 noteMovement)
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.anchoredPosition -= noteMovement;
    }

    public void ResetPathPosition()
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.anchoredPosition = Vector2.zero;
    }

    public void PointPathToward(GameObject target)
    {
        if (target != null)
        {
            UIUtils.PointToward(self: pathToPreviousNote,
                selfPos: GetComponent<RectTransform>()
                    .anchoredPosition,
                targetPos: target.GetComponent<RectTransform>()
                    .anchoredPosition);
        }
        else
        {
            pathToPreviousNote.sizeDelta = Vector2.zero;
            pathToPreviousNote.localRotation = Quaternion.identity;
        }

        if (target != null &&
            target.GetComponent<NoteObject>().note.type
                == NoteType.ChainHead)
        {
            UIUtils.RotateToward(
                self: target.GetComponent<NoteInEditor>().noteImage,
                selfPos: target.GetComponent<RectTransform>()
                    .anchoredPosition,
                targetPos: GetComponent<RectTransform>()
                    .anchoredPosition);
        }
    }
    #endregion

    #region Duration Trail
    public void ResetTrail()
    {
        int duration = (GetComponent<NoteObject>().note as 
            HoldNote).duration;
        float width = duration * (PatternPanel.ScanWidth /
            EditorContext.Pattern.patternMetadata.bps /
            Pattern.pulsesPerBeat);
        durationTrail.sizeDelta = new Vector2(width, 0f);
        invisibleTrail.sizeDelta = new Vector2(width, 0f);

        ToggleNoteImageTransparency();
    }

    public void RecordTrailActualLength()
    {
        trailActualLength = durationTrail.sizeDelta.x;
    }

    // During adjustment, this is the trail's "backend" length,
    // which may become negative; shown length is capped at 0.
    // Both lengths are only visual.
    private float trailActualLength;
    // Only visual; does not affect note duration.
    public void AdjustTrailLength(float delta)
    {
        trailActualLength += delta;
        float trailVisualLength = Mathf.Max(0f, trailActualLength);
        durationTrail.sizeDelta = new Vector2(trailVisualLength, 0f);
        invisibleTrail.sizeDelta = new Vector2(trailVisualLength, 0f);
        ToggleNoteImageTransparency();
    }

    private void ToggleNoteImageTransparency()
    {
        if (!transparentNoteImageWhenTrailTooShort) return;

        if (invisibleTrail.sizeDelta.x <
            GetComponent<RectTransform>().sizeDelta.x * 0.5f)
        {
            noteImage.GetComponent<Image>().color =
                new Color(1f, 1f, 1f, 0.38f);
        }
        else
        {
            noteImage.GetComponent<Image>().color = Color.white;
        }
    }
    #endregion

    #region Curve
    // All positions relative to note head.
    private List<Vector2> pointsOnCurve;

    public IList<Vector2> GetVisiblePointsOnCurve()
    {
        return pointsOnCurve;
    }

    // This does not render anchors and control points; call
    // ResetAnchorsAndControlPoints for that.
    public void ResetCurve()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        pointsOnCurve = new List<Vector2>();

        foreach (FloatPoint p in dragNote.Interpolate())
        {
            Vector2 pointOnCurve = new Vector2(
                p.pulse * PatternPanel.PulseWidth,
                -p.lane * PatternPanel.LaneHeight);
            pointsOnCurve.Add(pointOnCurve);
        }
        // TODO: do we need to smooth these points?

        // Rotate note head.
        UIUtils.RotateToward(self: noteImage,
            selfPos: pointsOnCurve[0],
            targetPos: pointsOnCurve[1]);

        // Draw curve.
        curvedImage.scale = 1f;
        curvedImage.SetVerticesDirty();

        // Draw new anchor receivers. Reuse them if applicable.
        for (int i = 0;
            i < anchorReceiverContainer.childCount;
            i++)
        {
            anchorReceiverContainer.GetChild(i).gameObject
                .SetActive(false);
        }
        if (anchorReceiverTemplate.transform.GetSiblingIndex()
            != 0)
        {
            anchorReceiverTemplate.transform.SetAsFirstSibling();
        }
        for (int i = 0; i < pointsOnCurve.Count - 1; i++)
        {
            int childIndex = i + 1;
            while (anchorReceiverContainer.childCount - 1
                < childIndex)
            {
                Instantiate(
                    anchorReceiverTemplate,
                    parent: anchorReceiverContainer);
            }
            RectTransform receiver =
                anchorReceiverContainer.GetChild(childIndex)
                .GetComponent<RectTransform>();
            receiver.gameObject.SetActive(true);
            receiver.anchoredPosition = pointsOnCurve[i];

            UIUtils.PointToward(receiver,
                selfPos: pointsOnCurve[i],
                targetPos: pointsOnCurve[i + 1]);
        }
    }

    public void ResetAllAnchorsAndControlPoints()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;

        for (int i = 0; i < anchorContainer.childCount; i++)
        {
            if (anchorContainer.GetChild(i).gameObject !=
                anchorTemplate)
            {
                Destroy(anchorContainer.GetChild(i).gameObject);
            }
        }
        for (int i = 0; i < dragNote.nodes.Count; i++)
        {
            DragNode dragNode = dragNote.nodes[i];

            GameObject anchor = Instantiate(anchorTemplate,
                parent: anchorContainer);
            anchor.SetActive(true);
            anchor.GetComponent<DragNoteAnchor>().anchorIndex = i;
            anchor.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(
                    dragNode.anchor.pulse * PatternPanel.PulseWidth,
                    -dragNode.anchor.lane * PatternPanel.LaneHeight);

            for (int control = 0; control < 2; control++)
            {
                ResetControlPointPosition(dragNode,
                    anchor, control);
            }

            ResetPathsToControlPoints(
                anchor.GetComponent<DragNoteAnchor>());
        }
    }

    public void ResetControlPointPosition(DragNode dragNode,
        GameObject anchor,
        int index)
    {
        FloatPoint point = dragNode.GetControlPoint(index);
        if ((GetNote() as DragNote).curveType == CurveType.BSpline)
        {
            point = new FloatPoint(0f, 0f);
        }
        Vector2 position = new Vector2(
            point.pulse * PatternPanel.PulseWidth,
            -point.lane * PatternPanel.LaneHeight);
        anchor.GetComponent<DragNoteAnchor>()
            .GetControlPoint(index)
            .GetComponent<RectTransform>()
            .anchoredPosition = position;
    }

    public void ResetPathsToControlPoints(DragNoteAnchor anchor)
    {
        for (int control = 0; control < 2; control++)
        {
            Vector2 position = anchor.GetControlPoint(control)
                .GetComponent<RectTransform>().anchoredPosition;
            RectTransform path = anchor
                .GetPathToControlPoint(control);
            UIUtils.PointToward(path,
                selfPos: Vector2.zero,
                targetPos: position);
        }
    }

    public bool ClickLandsOnCurve(Vector3 screenPosition)
    {
        // Prune
        if (screenPosition.x < noteImage.transform.position.x
            - PatternPanel.LaneHeight * 0.5f)
        {
            return false;
        }
        if (screenPosition.x > noteImage.transform.position.x
            + pointsOnCurve[pointsOnCurve.Count - 1].x
            + PatternPanel.LaneHeight * 0.5f)
        {
            return false;
        }

        // Should be laneHeight^2 * 0.25f, but leave some room for
        // error.
        float minSquaredDistance = PatternPanel.LaneHeight * 
            PatternPanel.LaneHeight * 0.3f;
        for (int i = 0; i < pointsOnCurve.Count - 1; i++)
        {
            float squaredDistance = 
                SquaredDistanceFromPointToLineSegment(
                    pointsOnCurve[i],
                    pointsOnCurve[i + 1],
                    screenPosition - noteImage.transform.position);
            if (squaredDistance <= minSquaredDistance)
            {
                return true;
            }
        }

        return false;
    }

    private float SquaredDistanceFromPointToLineSegment(
        Vector2 v, Vector2 w, Vector2 p)
    {
        // https://stackoverflow.com/a/1501725
        float l2 = (v - w).sqrMagnitude;
        float t = Mathf.Clamp01(Vector2.Dot(p - v, w - v) / l2);
        Vector2 projection = v + t * (w - v);
        return (p - projection).sqrMagnitude;
    }
    #endregion
}
