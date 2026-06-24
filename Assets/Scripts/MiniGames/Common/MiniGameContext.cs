using System;
using TMPro;
using UnityEngine;

public class MiniGameContext
{
    public Transform CanvasRoot;
    public TMP_FontAsset ProjectPixelFont;
    public Texture2D RunnerBackgroundTexture;
    public Texture2D RunnerCatTexture;
    public Texture2D RunnerJumpingCatTexture;
    public Texture2D RunnerBallTexture;
    public Texture2D RunnerBoxTexture;
    public Texture2D RunnerDebtTexture;
    public Texture2D FakeCaptchaTauntingCatTexture;
    public Action<string> SetStatus;
    public Func<string> GetBattleStats;
    public Action ClearActivePanel;
    public Action<float> ExitAfterDelay;
    public Action<MiniGameDefinition> ApplySuccess;
}
