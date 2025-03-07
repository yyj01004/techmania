using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpriteSheet
{
    public string filename;
    public int rows;
    public int columns;
    public int firstIndex;
    public int lastIndex;
    public int padding;
    public bool bilinearFilter;

    // Not used by all skins

    public float scale;  // Relative to 1x lane height
    public float speed;  // Relative to 60 fps
    public bool additiveShader;

    [NonSerialized]  // Loaded at runtime
    public Texture2D texture;
    [NonSerialized]
    public List<Sprite> sprites;

    public SpriteSheet()
    {
        rows = 1;
        columns = 1;
        firstIndex = 0;
        lastIndex = 0;
        padding = 0;
        bilinearFilter = true;

        scale = 1f;
        speed = 1f;
        additiveShader = false;
    }

    // Call after loading texture.
    public void GenerateSprites()
    {
        if (texture == null)
        {
            throw new Exception("Texture not yet loaded.");
        }
        texture.filterMode = bilinearFilter ? FilterMode.Bilinear :
            FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        sprites = new List<Sprite>();
        float spriteWidth = (float)texture.width / columns;
        float spriteHeight = (float)texture.height / rows;
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            int row = i / columns;
            // Unity thinks (0, 0) is bottom left but we think
            // (0, 0) is top left. So we inverse y here.
            int inverseRow = rows - 1 - row;
            int column = i % columns;
            Sprite s = Sprite.Create(texture,
                new Rect(column * spriteWidth + padding,
                    inverseRow * spriteHeight + padding,
                    spriteWidth - padding * 2,
                    spriteHeight - padding * 2),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                // The default is Tight, whose performance is
                // atrocious.
                meshType: SpriteMeshType.FullRect);
            sprites.Add(s);
        }
    }

    // For animations that cycle once per beat/scan, pass in
    // the float beat/scan number.
    // Integer part of the input number is removed.
    public Sprite GetSpriteAtFloatIndex(float floatIndex)
    {
        if (sprites == null) return null;
        floatIndex = floatIndex - Mathf.Floor(floatIndex);
        int index = Mathf.FloorToInt(floatIndex * sprites.Count);
        index = Mathf.Clamp(index, 0, sprites.Count - 1);
        return sprites[index];
    }

    // For animations that cycle on a fixed time. Relies on speed.
    // Returns null if the end of animation is reached.
    public Sprite GetSpriteForTime(float time, bool loop)
    {
        if (sprites == null) return null;
        float fps = 60f * speed;
        int index = Mathf.FloorToInt(time * fps);
        if (loop)
        {
            while (index < 0) index += sprites.Count;
            index = index % sprites.Count;
        }
        if (index < 0 || index >= sprites.Count) return null;
        return sprites[index];
    }

    #region Empty sprite sheet
    // Used in place of null sprite sheets when a skin is missing
    // items.
    public static Texture2D emptyTexture;
    public static void PrepareEmptySpriteSheet()
    {
        emptyTexture = new Texture2D(1, 1);
        emptyTexture.SetPixel(0, 0, Color.clear);
        emptyTexture.Apply();
    }

    public void MakeEmpty()
    {
        texture = emptyTexture;
        GenerateSprites();
    }

    public static SpriteSheet MakeNewEmptySpriteSheet()
    {
        SpriteSheet s = new SpriteSheet();
        s.MakeEmpty();
        return s;
    }
    #endregion
}

[Serializable]
[FormatVersion(NoteSkin.kVersion, typeof(NoteSkin), isLatest: true)]
public class NoteSkinBase : SerializableClass<NoteSkinBase> {}

// Most sprite sheets use scale, except for the "...end"s.
[Serializable]
public class NoteSkin : NoteSkinBase
{
    public const string kVersion = "1";
    public string author;

    // Note skin's name is the folder's name.

    public SpriteSheet basic;

    public SpriteSheet chainHead;
    public SpriteSheet chainNode;
    public SpriteSheet chainPath;

    public SpriteSheet dragHead;
    public SpriteSheet dragCurve;

    public SpriteSheet holdHead;
    public SpriteSheet holdTrail;
    public SpriteSheet holdTrailEnd;
    public SpriteSheet holdOngoingTrail;

    public SpriteSheet repeatHead;
    public SpriteSheet repeat;
    public SpriteSheet repeatHoldTrail;
    public SpriteSheet repeatHoldTrailEnd;
    public SpriteSheet repeatPath;
    public SpriteSheet repeatPathEnd;

    public NoteSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(basic);

        list.Add(chainHead);
        list.Add(chainNode);
        list.Add(chainPath);

        list.Add(dragHead);
        list.Add(dragCurve);

        list.Add(holdHead);
        list.Add(holdTrail);
        list.Add(holdTrailEnd);
        list.Add(holdOngoingTrail);

        list.Add(repeatHead);
        list.Add(repeat);
        list.Add(repeatHoldTrail);
        list.Add(repeatHoldTrailEnd);
        list.Add(repeatPath);
        list.Add(repeatPathEnd);

        return list;
    }
}

[Serializable]
[FormatVersion(VfxSkin.kVersion, typeof(VfxSkin), isLatest: true)]
public class VfxSkinBase : SerializableClass<VfxSkinBase> { }

// All sprite sheets use scale, speed and additiveShader.
[Serializable]
public class VfxSkin : VfxSkinBase
{
    public const string kVersion = "1";
    public string author;

    // VFX skin's name is the folder's name.
    // Each VFX (except for feverOverlay) is defined as multiple
    // layers of sprite sheets, each element in List corresponding
    // to one layer.

    public SpriteSheet feverOverlay;

    public List<SpriteSheet> basicMax;
    public List<SpriteSheet> basicCool;
    public List<SpriteSheet> basicGood;

    public List<SpriteSheet> dragOngoing;
    public List<SpriteSheet> dragComplete;

    public List<SpriteSheet> holdOngoingHead;
    public List<SpriteSheet> holdOngoingTrail;
    public List<SpriteSheet> holdComplete;

    public List<SpriteSheet> repeatHead;
    public List<SpriteSheet> repeatNote;
    public List<SpriteSheet> repeatHoldOngoingHead;
    public List<SpriteSheet> repeatHoldOngoingTrail;
    public List<SpriteSheet> repeatHoldComplete;

    public VfxSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(feverOverlay);

        list.AddRange(basicMax);
        list.AddRange(basicCool);
        list.AddRange(basicGood);

        list.AddRange(dragOngoing);
        list.AddRange(dragComplete);

        list.AddRange(holdOngoingHead);
        list.AddRange(holdOngoingTrail);
        list.AddRange(holdComplete);

        list.AddRange(repeatHead);
        list.AddRange(repeatNote);
        list.AddRange(repeatHoldOngoingHead);
        list.AddRange(repeatHoldOngoingTrail);
        list.AddRange(repeatHoldComplete);

        return list;
    }
}

[Serializable]
[FormatVersion(ComboSkin.kVersion, typeof(ComboSkin), isLatest: true)]
public class ComboSkinBase : SerializableClass<ComboSkinBase> { }

// All sprite sheets use speed.
[Serializable]
public class ComboSkin : ComboSkinBase
{
    public const string kVersion = "1";
    public string author;

    // Combo skin's name is the folder's name.

    public float distanceToNote;  // In pixels
    public float height;  // In pixels
    public float spaceBetweenJudgementAndCombo;  // In pixels

    public SpriteSheet feverMaxJudgement;
    public SpriteSheet rainbowMaxJudgement;
    public SpriteSheet maxJudgement;
    public SpriteSheet coolJudgement;
    public SpriteSheet goodJudgement;
    public SpriteSheet missJudgement;
    public SpriteSheet breakJudgement;

    public List<SpriteSheet> feverMaxDigits;
    public List<SpriteSheet> rainbowMaxDigits;
    public List<SpriteSheet> maxDigits;
    public List<SpriteSheet> coolDigits;
    public List<SpriteSheet> goodDigits;

    public ComboSkin()
    {
        version = kVersion;
    }

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(feverMaxJudgement);
        list.Add(rainbowMaxJudgement);
        list.Add(maxJudgement);
        list.Add(coolJudgement);
        list.Add(goodJudgement);
        list.Add(missJudgement);
        list.Add(breakJudgement);

        feverMaxDigits.ForEach(s => list.Add(s));
        rainbowMaxDigits.ForEach(s => list.Add(s));
        maxDigits.ForEach(s => list.Add(s));
        coolDigits.ForEach(s => list.Add(s));
        goodDigits.ForEach(s => list.Add(s));

        return list;
    }

    public List<List<SpriteSheet>> GetReferenceToDigitLists()
    {
        List<List<SpriteSheet>> list = new List<List<SpriteSheet>>();
        list.Add(feverMaxDigits);
        list.Add(rainbowMaxDigits);
        list.Add(maxDigits);
        list.Add(coolDigits);
        list.Add(goodDigits);
        return list;
    }
}

[Serializable]
[FormatVersion(GameUISkin.kVersion, typeof(GameUISkin),
    isLatest: true)]
public class GameUISkinBase : SerializableClass<GameUISkinBase> { }

[Serializable]
public class GameUISkin : GameUISkinBase
{
    public const string kVersion = "1";
    public string author;

    // Scanline animations play one cycle per beat.
    public SpriteSheet scanline;
    public SpriteSheet autoPlayScanline;

    // Plays through the last 3 beats of every scan (or last 3
    // half-beats or quarter-beats, if bps is low).
    // Background is flipped for right-to-left scans, number is not.
    // These two sprite sheets use additiveShader.
    public SpriteSheet scanCountdownBackground;
    public SpriteSheet scanCountdownNumbers;

    // Uses speed and additiveShader.
    public SpriteSheet touchClickFeedback;
    public float touchClickFeedbackSize;  // In pixels
    // Scaled to fill the entire lane. Uses speed and additiveShader.
    public SpriteSheet keystrokeFeedback;

    // Uses scale.
    public SpriteSheet approachOverlay;

    public List<SpriteSheet> GetReferenceToAllSpriteSheets()
    {
        List<SpriteSheet> list = new List<SpriteSheet>();

        list.Add(scanline);
        list.Add(autoPlayScanline);
        list.Add(scanCountdownBackground);
        list.Add(scanCountdownNumbers);
        list.Add(touchClickFeedback);
        list.Add(keystrokeFeedback);
        list.Add(approachOverlay);

        return list;
    }
}