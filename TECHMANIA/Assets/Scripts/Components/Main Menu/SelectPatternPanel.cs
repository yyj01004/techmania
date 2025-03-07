using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectPatternPanel : MonoBehaviour
{
    public GameObject backButton;
    public PreviewTrackPlayer previewPlayer;

    [Header("Track details")]
    public EyecatchSelfLoader eyecatchImage;
    public ScrollingText trackDetailsScrollingText;
    public TextMeshProUGUI genreText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;

    [Header("Pattern list")]
    public PatternRadioList patternList;

    [Header("Pattern details")]
    public ScrollingText authorText;
    public TextMeshProUGUI lengthText;
    public TextMeshProUGUI notesText;
    public ScrollingText modifiersText;
    public ScrollingText specialModifiersText;

    [Header("Buttons")]
    public ModifierSidesheet modifierSidesheet;
    public Button playButton;

    private void OnEnable()
    {
        // Show track details.
        Track track = GameSetup.track;
        eyecatchImage.LoadImage(GameSetup.trackFolder,
            track.trackMetadata);
        genreText.text = track.trackMetadata.genre;
        titleText.text = track.trackMetadata.title;
        artistText.text = track.trackMetadata.artist;
        trackDetailsScrollingText.SetUp();

        // Initialize pattern list.
        GameObject firstObject =
            patternList.InitializeAndReturnFirstPatternObject(track);
        PatternRadioList.SelectedPatternChanged += 
            OnSelectedPatternObjectChanged;

        // Other UI elements.
        ModifierSidesheet.ModifierChanged += OnModifierChanged;
        OnModifierChanged();
        RefreshPatternDetails(p: null);
        if (firstObject == null)
        {
            firstObject = backButton.gameObject;
        }
        EventSystem.current.SetSelectedGameObject(firstObject);

        // Play preview.
        previewPlayer.Play(GameSetup.trackFolder,
            GameSetup.track.trackMetadata,
            loop: true);
    }

    private void OnDisable()
    {
        PatternRadioList.SelectedPatternChanged -= 
            OnSelectedPatternObjectChanged;
        ModifierSidesheet.ModifierChanged -=
            OnModifierChanged;
        previewPlayer.Stop();
    }

    private void Update()
    {
        // Synchronize alpha with sidesheet because the
        // CanvasGroup on the sidesheet ignores parent.
        if (PanelTransitioner.transitioning &&
            modifierSidesheet.gameObject.activeSelf)
        {
            modifierSidesheet.GetComponent<CanvasGroup>().alpha
                = GetComponent<CanvasGroup>().alpha;
        }
    }

    private void RefreshPatternDetails(Pattern p)
    {
        if (p == null)
        {
            authorText.SetUp("-");
            lengthText.text = "-";
            notesText.text = "-";
            playButton.interactable = false;
        }
        else
        {
            p.PrepareForTimeCalculation();
            float length = p.GetLengthInSeconds();

            authorText.SetUp(p.patternMetadata.author);
            lengthText.text = UIUtils.FormatTime(length,
                includeMillisecond: false);
            notesText.text = p.NumPlayableNotes().ToString();
            playButton.interactable = true;
        }
    }

    private void OnSelectedPatternObjectChanged(Pattern p)
    {
        RefreshPatternDetails(p);
    }

    private void OnModifierChanged()
    {
        string modifierLine1, modifierLine2;
        modifierSidesheet.GetModifierDisplay(
            out modifierLine1, out modifierLine2);

        modifiersText.SetUp(modifierLine1);
        specialModifiersText.SetUp(modifierLine2);
    }

    public void OnModifierButtonClick()
    {
        modifierSidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    public void OnPlayButtonClick()
    {
        previewPlayer.Stop();

        // Create a clone of the pattern with modifiers applied.
        // Game will operate on the clone.
        // The original pattern is kept in memory so its GUID and
        // fingerprint are still available later.
        GameSetup.patternBeforeApplyingModifier = 
            patternList.GetSelectedPattern();
        if (GameSetup.patternBeforeApplyingModifier == null)
            return;
        GameSetup.pattern = GameSetup.patternBeforeApplyingModifier
            .ApplyModifiers(Modifiers.instance);

        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            Modifiers.instance.mode = Modifiers.Mode.NoFail;
            OnModifierChanged();
        }
        if (Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift))
        {
            Modifiers.instance.mode = Modifiers.Mode.AutoPlay;
            OnModifierChanged();
        }

        // Save to disk because the game scene will reload options.
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());

        Curtain.DrawCurtainThenGoToScene("Game");
    }
}
