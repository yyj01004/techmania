﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackSetupPanel : MonoBehaviour
{
    public MessageDialog messageDialog;
    public ConfirmDialog confirmDialog;
    public Tabs tabs;

    #region Filename caching
    private List<string> audioFilesCache;
    private List<string> imageFilesCache;
    private List<string> videoFilesCache;

    private void RefreshFilenameCaches()
    {
        audioFilesCache = Paths.GetAllAudioFiles(
            EditorContext.trackFolder);
        imageFilesCache = Paths.GetAllImageFiles(
            EditorContext.trackFolder);
        videoFilesCache = Paths.GetAllVideoFiles(
            EditorContext.trackFolder);
    }
    #endregion

    #region Refreshing
    private void OnEnable()
    {
        Tabs.tabChanged += Refresh;
        EditorContext.UndoInvoked += OnUndoOrRedo;
        EditorContext.RedoInvoked += OnUndoOrRedo;
        PatternRadioList.SelectedPatternChanged += 
            SelectedPatternChanged;

        selectedPattern = null;
        EditorContext.ClearUndoRedoStack();
        RefreshFilenameCaches();
        Refresh();
    }

    private void OnDisable()
    {
        Tabs.tabChanged -= Refresh;
        EditorContext.UndoInvoked -= OnUndoOrRedo;
        EditorContext.RedoInvoked -= OnUndoOrRedo;
        PatternRadioList.SelectedPatternChanged -= 
            SelectedPatternChanged;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            RefreshFilenameCaches();
            Refresh();
        }
    }

    private void OnUndoOrRedo(EditTransaction t)
    {
        Refresh();
    }

    private void Refresh()
    {
        switch (tabs.CurrentTab)
        {
            case 0:
                RefreshResourcesTab();
                break;
            case 1:
                RefreshMetadataTab();
                break;
            case 2:
                RefreshPatternsTab();
                break;
        }
    }
    #endregion

    #region Resources tab
    [Header("Resources tab")]
    public TextMeshProUGUI audioFilesDisplay;
    public TextMeshProUGUI imageFilesDisplay;
    public TextMeshProUGUI videoFilesDisplay;

    public void OnImportButtonClick()
    {
        SFB.ExtensionFilter[] extensionFilters =
            new SFB.ExtensionFilter[2];
        extensionFilters[0] = new SFB.ExtensionFilter(
            Locale.GetString(
                "track_setup_resource_tab_import_dialog_supported_formats"),
            new string[] {
                "wav",
                "ogg",
                "jpg",
                "png",
                "mp4",
                "wmv"
            });
        extensionFilters[1] = new SFB.ExtensionFilter(
            Locale.GetString(
                "track_setup_resource_tab_import_dialog_all_files"),
            new string[] { "*" });
        string[] sources = SFB.StandaloneFileBrowser.OpenFilePanel(
            Locale.GetString(
                "track_setup_resource_tab_import_dialog_title"),
            "",
            extensionFilters, multiselect: true);
        string trackFolder = EditorContext.trackFolder;

        List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
        List<string> filesToBeOverwritten = new List<string>();
        foreach (string source in sources)
        {
            FileInfo fileInfo = new FileInfo(source);
            if (fileInfo.DirectoryName == trackFolder) continue;
            string destination = Path.Combine(trackFolder, fileInfo.Name);

            if (File.Exists(destination))
            {
                filesToBeOverwritten.Add(fileInfo.Name);
            }
            pairs.Add(new Tuple<string, string>(source, destination));
        }

        if (filesToBeOverwritten.Count > 0)
        {
            string fileList = "";
            for (int i = 0; i < filesToBeOverwritten.Count; i++)
            {
                if (i == 10)
                {
                    fileList += "\n";
                    fileList += Locale.GetStringAndFormat(
                        "track_setup_resource_tab_overwrite_omitted_files",
                        filesToBeOverwritten.Count - 10);
                    break;
                }
                else
                {
                    if (fileList != "") fileList += "\n";
                    fileList += filesToBeOverwritten[i];
                }
            }
            confirmDialog.Show(
                Locale.GetStringAndFormat(
                    "track_setup_resource_tab_overwrite_warning",
                    fileList),
                Locale.GetString(
                    "track_setup_resource_tab_overwrite_confirm"),
                Locale.GetString(
                    "track_setup_resource_tab_overwrite_cancel"),
                () =>
                {
                    StartCopy(pairs);
                });
        }
        else
        {
            StartCopy(pairs);
        }
    }

    private void StartCopy(List<Tuple<string, string>> pairs)
    {
        foreach (Tuple<string, string> pair in pairs)
        {
            try
            {
                File.Copy(pair.Item1, pair.Item2, overwrite: true);
            }
            catch (Exception e)
            {
                messageDialog.Show(
                    Locale.GetStringAndFormat(
                        "track_setup_resource_tab_import_error_format",
                        pair.Item1,
                        pair.Item2,
                        e.Message));
                break;
            }
        }

        RefreshFilenameCaches();
        RefreshResourcesTab();
    }

    private string CondenseFileList(List<string> fullPaths)
    {
        string str = "";
        foreach (string file in fullPaths)
        {
            str += new FileInfo(file).Name + "\n";
        }
        return str.TrimEnd('\n');
    }

    public void RefreshResourcesTab()
    {
        audioFilesDisplay.text = CondenseFileList(
            audioFilesCache);
        imageFilesDisplay.text = CondenseFileList(
            imageFilesCache);
        videoFilesDisplay.text = CondenseFileList(
            videoFilesCache);
    }
    #endregion

    #region Metadata tab
    [Header("Metadata tab")]
    public TMP_InputField trackTitle;
    public TMP_InputField artist;
    public TMP_InputField genre;
    public TMP_InputField additionalCredits;
    public EyecatchSelfLoader eyecatchPreview;
    public TMP_Dropdown eyecatchImage;
    public TMP_Dropdown previewTrack;
    public TMP_InputField startTime;
    public TMP_InputField endTime;
    public PreviewTrackPlayer previewTrackPlayer;

    public void RefreshMetadataTab()
    {
        TrackMetadata metadata = EditorContext.track.trackMetadata;

        trackTitle.SetTextWithoutNotify(metadata.title);
        artist.SetTextWithoutNotify(metadata.artist);
        genre.SetTextWithoutNotify(metadata.genre);
        additionalCredits.SetTextWithoutNotify(
            metadata.additionalCredits);

        UIUtils.MemoryToDropdown(eyecatchImage,
            metadata.eyecatchImage, imageFilesCache);
        UIUtils.MemoryToDropdown(previewTrack,
            metadata.previewTrack, audioFilesCache);
        startTime.SetTextWithoutNotify(metadata.previewStartTime.ToString());
        endTime.SetTextWithoutNotify(metadata.previewEndTime.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            trackTitle,
            artist,
            genre,
            additionalCredits,
            startTime,
            endTime
        })
        {
            field.GetComponent<MaterialTextField>().RefreshMiniLabel();
        }

        RefreshEyecatchPreview();
    }

    public void OnMetadataUpdated()
    {
        TrackMetadata metadata = EditorContext.track.trackMetadata;
        bool madeChange = false;

        UIUtils.UpdateMetadataInMemory(
            ref metadata.title, trackTitle.text, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref metadata.artist, artist.text, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref metadata.genre, genre.text, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref metadata.additionalCredits,
            additionalCredits.text, ref madeChange);

        UIUtils.UpdateMetadataInMemory(
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref metadata.previewTrack, previewTrack, ref madeChange);
        UIUtils.ClampInputField(startTime, 0.0, double.MaxValue);
        UIUtils.UpdateMetadataInMemory(
            ref metadata.previewStartTime, startTime.text, ref madeChange);
        UIUtils.ClampInputField(endTime, 0.0, double.MaxValue);
        UIUtils.UpdateMetadataInMemory(
            ref metadata.previewEndTime, endTime.text, ref madeChange);
    }

    public void OnEyecatchUpdated()
    {
        RefreshEyecatchPreview();
    }

    public void RefreshEyecatchPreview()
    {
        eyecatchPreview.LoadImage(EditorContext.trackFolder,
            EditorContext.track.trackMetadata);
    }

    public void OnPlayPreviewButtonClick()
    {
        previewTrackPlayer.Play(
            EditorContext.trackFolder,
            EditorContext.track.trackMetadata,
            loop: false);
    }

    public void OnDeleteTrackButtonClick()
    {
        confirmDialog.Show(
            Locale.GetStringAndFormat(
                "track_setup_metadata_tab_delete_track_confirmation",
                EditorContext.trackFolder),
            Locale.GetString(
                "track_setup_metadata_tab_delete_track_confirm"),
            Locale.GetString(
                "track_setup_metadata_tab_delete_track_cancel"),
            () =>
            {
                Directory.Delete(EditorContext.trackFolder,
                    recursive: true);
                SelectTrackPanel.RemoveCachedListsAtCurrentLocation();
                GetComponentInChildren<
                    CustomTransitionFromTrackSetupPanel>().
                ForceTransition();
            });
    }

    #endregion

    #region Patterns tab
    [Header("Patterns tab")]
    public PatternRadioList patternList;
    public GameObject patternMetadata;
    public GameObject patternButtons;
    public GameObject noPatternSelectedNotice;
    public TMP_InputField patternName;
    public TMP_InputField patternAuthor;
    public TMP_Dropdown controlScheme;
    public TMP_InputField patternLevel;
    public TMP_Dropdown patternBackingTrack;
    public TMP_Dropdown backgroundImage;
    public TMP_Dropdown backgroundVideo;
    public TMP_InputField bgaOffset;
    public Toggle waitForEndOfBga;
    public Toggle playBgaOnLoop;
    public TMP_InputField firstBeatOffset;
    public TMP_InputField initialBpm;
    public TMP_InputField bps;
    public TransitionToPanel transitionToPatternPanel;

    // Keep a reference to the selected pattern, so we can
    // re-select it when the pattern list refreshes.
    //
    // However this reference will be invalidated upon undo/redo.
    // Must refresh it upon undo/redo.
    private Pattern selectedPattern;

    private void RefreshPatternsTab()
    {
        if (selectedPattern != null)
        {
            selectedPattern = EditorContext.track.FindPatternByGuid(
                selectedPattern.patternMetadata.guid);
        }
        RefreshPatternList();
        RefreshPatternMetadata();
    }

    private void RefreshPatternList()
    {
        patternList.InitializeAndReturnFirstPatternObject(
            EditorContext.track, selectedPattern);
    }

    private void RefreshPatternMetadata()
    {
        patternMetadata.SetActive(selectedPattern != null);
        patternButtons.SetActive(selectedPattern != null);
        noPatternSelectedNotice.SetActive(selectedPattern == null);
        if (selectedPattern == null) return;

        PatternMetadata m = selectedPattern.patternMetadata;
        patternName.SetTextWithoutNotify(m.patternName);
        patternAuthor.SetTextWithoutNotify(m.author);
        controlScheme.options.Clear();
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            Locale.GetString(
                "track_setup_patterns_tab_control_scheme_touch")));
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            Locale.GetString(
                "track_setup_patterns_tab_control_scheme_keys")));
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            Locale.GetString(
                "track_setup_patterns_tab_control_scheme_km")));
        controlScheme.SetValueWithoutNotify((int)m.controlScheme);
        controlScheme.RefreshShownValue();
        patternLevel.SetTextWithoutNotify(m.level.ToString());

        UIUtils.MemoryToDropdown(patternBackingTrack,
            m.backingTrack, audioFilesCache);
        UIUtils.MemoryToDropdown(backgroundImage,
            m.backImage, imageFilesCache);
        UIUtils.MemoryToDropdown(backgroundVideo,
            m.bga, videoFilesCache);
        bgaOffset.SetTextWithoutNotify(m.bgaOffset.ToString());
        waitForEndOfBga.SetIsOnWithoutNotify(m.waitForEndOfBga);
        waitForEndOfBga.interactable = !m.playBgaOnLoop;
        playBgaOnLoop.SetIsOnWithoutNotify(m.playBgaOnLoop);

        firstBeatOffset.SetTextWithoutNotify(m.firstBeatOffset.ToString());
        initialBpm.SetTextWithoutNotify(m.initBpm.ToString());
        bps.SetTextWithoutNotify(m.bps.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            patternName,
            patternAuthor,
            patternLevel,
            bgaOffset,
            firstBeatOffset,
            initialBpm,
            bps
        })
        {
            field.GetComponent<MaterialTextField>().RefreshMiniLabel();
        }
    }

    private void SelectedPatternChanged(Pattern newSelection)
    {
        selectedPattern = newSelection;
        RefreshPatternMetadata();
    }

    public void OnPatternMetadataChanged()
    {
        PatternMetadata m = selectedPattern.patternMetadata;
        bool madeChange = false;

        UIUtils.UpdateMetadataInMemory(ref m.patternName,
            patternName.text, ref madeChange);
        UIUtils.UpdateMetadataInMemory(ref m.author,
            patternAuthor.text, ref madeChange);
        UIUtils.ClampInputField(patternLevel,
            Pattern.minLevel, int.MaxValue);
        UIUtils.UpdateMetadataInMemory(ref m.level,
            patternLevel.text, ref madeChange);

        // Special handling for control scheme
        if ((int)m.controlScheme != controlScheme.value)
        {
            if (!madeChange)
            {
                EditorContext.PrepareToModifyMetadata();
                madeChange = true;
            }
            m.controlScheme = (ControlScheme)controlScheme.value;
        }

        UIUtils.UpdateMetadataInMemory(ref m.backingTrack,
            patternBackingTrack, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref m.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref m.bga, backgroundVideo, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref m.bgaOffset, bgaOffset.text, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref m.waitForEndOfBga, waitForEndOfBga.isOn, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref m.playBgaOnLoop, playBgaOnLoop.isOn, ref madeChange);
        UIUtils.UpdateMetadataInMemory(
            ref m.firstBeatOffset, firstBeatOffset.text,
            ref madeChange);
        UIUtils.ClampInputField(initialBpm,
            Pattern.minBpm, float.MaxValue);
        UIUtils.UpdateMetadataInMemory(
            ref m.initBpm, initialBpm.text, ref madeChange);
        UIUtils.ClampInputField(bps, Pattern.minBps, int.MaxValue);
        UIUtils.UpdateMetadataInMemory(
            ref m.bps, bps.text, ref madeChange);

        // Disable waitForEndOfBga if playBgaOnLoop is turned on.
        if (m.playBgaOnLoop)
        {
            m.waitForEndOfBga = false;
            waitForEndOfBga.SetIsOnWithoutNotify(false);
            waitForEndOfBga.interactable = false;
        }
        else
        {
            waitForEndOfBga.interactable = true;
        }

        if (madeChange)
        {
            EditorContext.track.SortPatterns();
            RefreshPatternList();
        }
    }

    public void OnNewPatternButtonClick()
    {
        EditorContext.PrepareToModifyMetadata();
        EditorContext.track.patterns.Add(new Pattern());
        EditorContext.track.SortPatterns();

        RefreshPatternList();
    }

    public void OnDeletePatternButtonClick()
    {
        // This is undoable, so no need for confirmation.
        EditorContext.PrepareToModifyMetadata();
        EditorContext.track.patterns.Remove(selectedPattern);

        selectedPattern = null;
        RefreshPatternList();
        RefreshPatternMetadata();
    }

    public void OnDuplicatePatternButtonClick()
    {
        EditorContext.PrepareToModifyMetadata();
        EditorContext.track.patterns.Add(selectedPattern.CloneWithDifferentGuid());
        EditorContext.track.SortPatterns();

        RefreshPatternList();
    }

    public void OnOpenPatternButtonClick()
    {
        int index = EditorContext.track.FindPatternIndexByGuid(
            selectedPattern.patternMetadata.guid);
        if (index < 0) return;
        EditorContext.patternIndex = index;

        transitionToPatternPanel.Invoke();
    }
    #endregion
}
