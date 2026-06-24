using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class BattleSceneBootstrap : MonoBehaviour
{
    private readonly List<MiniGameDefinition> miniGames = new List<MiniGameDefinition>();
    private Transform miniGameHost;
    private TMP_Text statusText;
    private GameObject activeMiniGamePanel;
    private IMiniGameController activeMiniGameController;
    private MiniGameContext miniGameContext;
    private Coroutine exitCoroutine;
    private int bossHp = 100;
    private int dominance;

    public TMP_FontAsset projectPixelFont;
    public Texture2D runnerBackgroundTexture;
    public Texture2D runnerCatTexture;
    public Texture2D runnerJumpingCatTexture;
    public Texture2D runnerBallTexture;
    public Texture2D runnerBoxTexture;
    public Texture2D runnerDebtTexture;
    public Texture2D fakeCaptchaTauntingCatTexture;

    private void Start()
    {
        miniGames.AddRange(Resources.LoadAll<MiniGameDefinition>("MiniGames"));

        if (miniGames.Count == 0)
        {
            Debug.LogWarning("No MiniGameDefinition assets were found in Resources/MiniGames.");
        }

        if (projectPixelFont == null)
        {
            projectPixelFont = TMP_Settings.defaultFontAsset;
        }

        if (projectPixelFont == null)
        {
            Debug.LogWarning("Could not find the project TMP default font asset.");
        }

        CreateEventSystem();
        CreateBattleUi();
        CreateMiniGameContext();
    }

    private void Update()
    {
        activeMiniGameController?.Tick(Time.deltaTime);
    }

    private void CreateBattleUi()
    {
        GameObject canvasObject = new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = MiniGameUi.CreatePanel(canvasObject.transform, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.62f, 0.66f, 0.72f, 1f));
        background.raycastTarget = false;

        TMP_Text title = MiniGameUi.CreateText(canvasObject.transform, "Title", "Cat Battle System", 56f, TextAlignmentOptions.Center, Color.white, projectPixelFont);
        MiniGameUi.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(720f, 80f));

        statusText = MiniGameUi.CreateText(canvasObject.transform, "BattleStatus", "Boss HP: 100    Dominance: 0", 32f, TextAlignmentOptions.Center, Color.white, projectPixelFont);
        MiniGameUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 255f), new Vector2(960f, 120f));

        Image miniGameFrame = MiniGameUi.CreatePanel(canvasObject.transform, "MiniGameFrame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.09f, 0.1f, 0.14f, 1f));
        MiniGameUi.SetRect(miniGameFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1120f, 540f));

        Image miniGameViewport = MiniGameUi.CreatePanel(miniGameFrame.transform, "MiniGameViewport", Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f), new Color(0.8f, 0.82f, 0.9f, 1f));
        miniGameViewport.gameObject.AddComponent<RectMask2D>();
        miniGameHost = miniGameViewport.transform;

        Button battleButton = MiniGameUi.CreateButton(canvasObject.transform, "BattleButton", "Battle", new Vector2(220f, 64f), 34f, projectPixelFont);
        MiniGameUi.SetRect((RectTransform)battleButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -460f), new Vector2(220f, 64f));
        battleButton.onClick.AddListener(RunSelectedMiniGame);
    }

    private void CreateMiniGameContext()
    {
        miniGameContext = new MiniGameContext
        {
            CanvasRoot = miniGameHost,
            ProjectPixelFont = projectPixelFont,
            RunnerBackgroundTexture = runnerBackgroundTexture,
            RunnerCatTexture = runnerCatTexture,
            RunnerJumpingCatTexture = runnerJumpingCatTexture,
            RunnerBallTexture = runnerBallTexture,
            RunnerBoxTexture = runnerBoxTexture,
            RunnerDebtTexture = runnerDebtTexture,
            FakeCaptchaTauntingCatTexture = fakeCaptchaTauntingCatTexture,
            SetStatus = value => statusText.text = value,
            GetBattleStats = GetBattleStats,
            ClearActivePanel = ClearActiveMiniGamePanel,
            ExitAfterDelay = StartExitAfterDelay,
            ApplySuccess = ApplyMiniGameSuccess
        };
    }

    private void RunSelectedMiniGame()
    {
        if (activeMiniGamePanel != null)
        {
            statusText.text = "Finish the current mini game first.";
            return;
        }

        if (miniGames.Count == 0)
        {
            statusText.text = $"No mini games left.\n{GetBattleStats()}";
            return;
        }

        int miniGameIndex = Random.Range(0, miniGames.Count);
        MiniGameDefinition miniGame = miniGames[miniGameIndex];
        miniGames.RemoveAt(miniGameIndex);
        ClearActiveMiniGamePanel();

        activeMiniGameController = CreateMiniGameController(miniGame.displayName);
        activeMiniGamePanel = activeMiniGameController.Start(miniGame, miniGameContext);
    }

    private IMiniGameController CreateMiniGameController(string displayName)
    {
        switch (displayName)
        {
            case "Chrome Cat Runner":
                return new ChromeCatRunnerMiniGame();
            case "Fake CAPTCHA":
                return new FakeCaptchaMiniGame();
            case "Google Verify Parody":
                return new GoogleVerifyParodyMiniGame();
            case "10 Second Spot the Difference":
                return new SpotDifferenceMiniGame();
            default:
                return new InstantMiniGameController();
        }
    }

    private void ApplyMiniGameSuccess(MiniGameDefinition miniGame)
    {
        bossHp = Mathf.Max(0, bossHp - miniGame.bossHpDamage);
        dominance += miniGame.dominanceGain;
        statusText.text = $"{miniGame.displayName}\nBoss HP -{miniGame.bossHpDamage}    Dominance +{miniGame.dominanceGain}\nBoss HP: {bossHp}    Dominance: {dominance}";
    }

    private string GetBattleStats()
    {
        return $"Boss HP: {bossHp}    Dominance: {dominance}";
    }

    private void ClearActiveMiniGamePanel()
    {
        if (exitCoroutine != null)
        {
            StopCoroutine(exitCoroutine);
            exitCoroutine = null;
        }

        activeMiniGameController?.Stop();
        activeMiniGameController = null;

        if (activeMiniGamePanel == null)
        {
            return;
        }

        Destroy(activeMiniGamePanel);
        activeMiniGamePanel = null;
    }

    private void StartExitAfterDelay(float seconds)
    {
        if (exitCoroutine != null)
        {
            StopCoroutine(exitCoroutine);
        }

        exitCoroutine = StartCoroutine(ClearActiveMiniGamePanelAfterDelay(seconds));
    }

    private IEnumerator ClearActiveMiniGamePanelAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        exitCoroutine = null;
        ClearActiveMiniGamePanel();
    }

    private void CreateEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
