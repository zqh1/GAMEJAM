using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChromeCatRunnerMiniGame : IMiniGameController
{
    private const float TrackWidth = 1000f;
    private const float TrackHeight = 500f;
    private const float JumpDuration = 1.15f;
    private const float JumpHeight = 160f;
    private const float ObstacleSpeed = 245f;
    private const float InitialObstacleLeadDistance = 230f;
    private const float MinObstacleSpacing = 400f;
    private const float MaxObstacleSpacing = 620f;
    private const float SurvivalSeconds = 10f;
    private const int CatAnimationFrameCount = 3;
    private const float CatAnimationFramesPerSecond = 9f;

    private readonly List<RunnerObstacle> obstacles = new List<RunnerObstacle>();
    private readonly List<RectTransform> backgroundSegments = new List<RectTransform>();
    private readonly Texture2D[] obstacleTextures = new Texture2D[3];
    private readonly string[] obstacleLabels = { "Yarn", "Box", "Bill" };
    private MiniGameDefinition miniGame;
    private MiniGameContext context;
    private RectTransform cat;
    private RawImage catImage;
    private TMP_Text messageText;
    private TMP_Text timerText;
    private float catBaseY = 180f;
    private float jumpTimer;
    private float elapsed;
    private float catAnimationTimer;
    private bool finished;

    private class RunnerObstacle
    {
        public RectTransform RectTransform;
        public RawImage Image;
        public string Label;
        public int TypeIndex;
    }

    public GameObject Start(MiniGameDefinition miniGame, MiniGameContext context)
    {
        this.miniGame = miniGame;
        this.context = context;

        context.SetStatus("Chrome Cat Runner\nSurvive for 10 seconds.");
        obstacleTextures[0] = context.RunnerBallTexture;
        obstacleTextures[1] = context.RunnerBoxTexture;
        obstacleTextures[2] = context.RunnerDebtTexture;

        GameObject panel = new GameObject("ChromeCatRunnerPanel", typeof(RectTransform));
        panel.transform.SetParent(context.CanvasRoot, false);

        RectTransform rootRect = panel.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image track = MiniGameUi.CreatePanel(panel.transform, "Track", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.89f, 0.9f, 1f, 1f));
        MiniGameUi.SetRect(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TrackWidth, TrackHeight));
        track.gameObject.AddComponent<RectMask2D>();

        CreateBackgroundSegment(track.transform, 0f);
        CreateBackgroundSegment(track.transform, TrackWidth);

        cat = CreateRawImage(track.transform, "Cat", context.RunnerCatTexture, new Vector2(145f, catBaseY), new Vector2(100f, 100f));
        catImage = cat.GetComponent<RawImage>();
        UpdateCatAnimationFrame(0);

        Button jumpButton = MiniGameUi.CreateButton(track.transform, "JumpButton", "Jump", new Vector2(130f, 52f), 26f, context.ProjectPixelFont);
        MiniGameUi.SetRect((RectTransform)jumpButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(130f, 52f));
        jumpButton.onClick.AddListener(Jump);

        messageText = MiniGameUi.CreateText(track.transform, "RunnerMessage", "Press <color=#ffb000><b>Space</b></color> to jump over yarn, boxes, and bills.", 26f, TextAlignmentOptions.Center, Color.black, context.ProjectPixelFont);
        MiniGameUi.SetRect(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(660f, 54f));

        timerText = MiniGameUi.CreateText(track.transform, "Timer", "10.0", 26f, TextAlignmentOptions.Center, Color.black, context.ProjectPixelFont);
        MiniGameUi.SetRect(timerText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -22f), new Vector2(110f, 48f));

        float obstacleX = TrackWidth + InitialObstacleLeadDistance;
        int obstacleIndex = GetRandomObstacleIndex(-1);
        CreateObstacle(track.transform, obstacleIndex, obstacleX);
        obstacleX += Random.Range(MinObstacleSpacing, MaxObstacleSpacing);
        obstacleIndex = GetRandomObstacleIndex(obstacleIndex);
        CreateObstacle(track.transform, obstacleIndex, obstacleX);
        obstacleX += Random.Range(MinObstacleSpacing, MaxObstacleSpacing);
        obstacleIndex = GetRandomObstacleIndex(obstacleIndex);
        CreateObstacle(track.transform, obstacleIndex, obstacleX);

        return panel;
    }

    public void Tick(float deltaTime)
    {
        if (finished)
        {
            return;
        }

        elapsed += deltaTime;
        timerText.text = Mathf.Max(0f, SurvivalSeconds - elapsed).ToString("0.0");

        UpdateInput();
        UpdateBackground(deltaTime);
        UpdateJump(deltaTime);
        UpdateCatVisual(deltaTime);
        UpdateObstacles(deltaTime);

        if (elapsed >= SurvivalSeconds)
        {
            Complete();
        }
    }

    public void Stop()
    {
        finished = true;
    }

    private void CreateBackgroundSegment(Transform parent, float startX)
    {
        GameObject segmentObject = new GameObject("RunnerBackgroundSegment", typeof(RectTransform), typeof(RawImage));
        segmentObject.transform.SetParent(parent, false);

        RectTransform segment = segmentObject.GetComponent<RectTransform>();
        MiniGameUi.SetRect(segment, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(startX, 0f), new Vector2(TrackWidth, TrackHeight));
        backgroundSegments.Add(segment);

        RawImage image = segmentObject.GetComponent<RawImage>();
        image.texture = context.RunnerBackgroundTexture;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void CreateObstacle(Transform parent, int obstacleIndex, float startX)
    {
        string label = obstacleLabels[obstacleIndex];
        Texture2D texture = obstacleTextures[obstacleIndex];
        RectTransform obstacleRect = CreateRawImage(parent, label, texture, new Vector2(startX, catBaseY - 4f), GetObstacleSize(obstacleIndex));

        obstacles.Add(new RunnerObstacle
        {
            RectTransform = obstacleRect,
            Image = obstacleRect.GetComponent<RawImage>(),
            Label = label,
            TypeIndex = obstacleIndex
        });
    }

    private RectTransform CreateRawImage(Transform parent, string name, Texture2D texture, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        MiniGameUi.SetRect(rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), position, size);

        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = texture;
        image.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        image.raycastTarget = false;

        return rectTransform;
    }

    private Vector2 GetObstacleSize(int obstacleIndex)
    {
        switch (obstacleIndex)
        {
            case 0:
                return new Vector2(80f, 65f);
            case 1:
                return new Vector2(72f, 72f);
            case 2:
                return new Vector2(64f, 80f);
            default:
                return new Vector2(66f, 66f);
        }
    }

    private void UpdateInput()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Jump();
        }
    }

    private void Jump()
    {
        if (finished)
        {
            return;
        }

        if (jumpTimer <= 0.05f)
        {
            jumpTimer = JumpDuration;
        }
    }

    private void UpdateBackground(float deltaTime)
    {
        foreach (RectTransform segment in backgroundSegments)
        {
            Vector2 position = segment.anchoredPosition;
            position.x -= 145f * deltaTime;

            if (position.x <= -TrackWidth)
            {
                position.x += TrackWidth * 2f;
            }

            segment.anchoredPosition = position;
        }
    }

    private void UpdateJump(float deltaTime)
    {
        if (jumpTimer > 0f)
        {
            jumpTimer = Mathf.Max(0f, jumpTimer - deltaTime);
            float jumpProgress = 1f - Mathf.Abs((jumpTimer / JumpDuration) * 2f - 1f);
            cat.anchoredPosition = new Vector2(cat.anchoredPosition.x, catBaseY + jumpProgress * JumpHeight);
            return;
        }

        cat.anchoredPosition = new Vector2(cat.anchoredPosition.x, catBaseY);
    }

    private void UpdateCatVisual(float deltaTime)
    {
        if (jumpTimer > 0f && context.RunnerJumpingCatTexture != null)
        {
            catImage.texture = context.RunnerJumpingCatTexture;
            catImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        catImage.texture = context.RunnerCatTexture;
        catAnimationTimer += deltaTime;
        int frameIndex = Mathf.FloorToInt(catAnimationTimer * CatAnimationFramesPerSecond) % CatAnimationFrameCount;
        UpdateCatAnimationFrame(frameIndex);
    }

    private void UpdateCatAnimationFrame(int frameIndex)
    {
        if (catImage == null)
        {
            return;
        }

        float frameWidth = 1f / CatAnimationFrameCount;
        catImage.uvRect = new Rect(frameWidth * frameIndex, 0f, frameWidth, 1f);
    }

    private void UpdateObstacles(float deltaTime)
    {
        foreach (RunnerObstacle obstacle in obstacles)
        {
            Vector2 position = obstacle.RectTransform.anchoredPosition;
            position.x -= ObstacleSpeed * deltaTime;

            if (position.x < -80f)
            {
                ConfigureObstacle(obstacle, GetRandomObstacleIndex(GetRightmostObstacleTypeIndex()));
                position.x = GetRightmostObstacleX() + Random.Range(MinObstacleSpacing, MaxObstacleSpacing);
            }

            obstacle.RectTransform.anchoredPosition = position;

            float distanceToCat = Mathf.Abs(position.x - cat.anchoredPosition.x);
            bool isCatLow = cat.anchoredPosition.y < catBaseY + 50f;

            if (distanceToCat < 62f && isCatLow)
            {
                Fail(obstacle.Label);
                return;
            }
        }
    }

    private void ConfigureObstacle(RunnerObstacle obstacle, int obstacleIndex)
    {
        obstacle.Label = obstacleLabels[obstacleIndex];
        obstacle.TypeIndex = obstacleIndex;
        obstacle.RectTransform.name = obstacle.Label;
        obstacle.RectTransform.sizeDelta = GetObstacleSize(obstacleIndex);

        if (obstacle.Image == null)
        {
            return;
        }

        Texture2D texture = obstacleTextures[obstacleIndex];
        obstacle.Image.texture = texture;
        obstacle.Image.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
    }

    private int GetRandomObstacleIndex(int excludedIndex)
    {
        if (obstacleTextures.Length <= 1)
        {
            return 0;
        }

        int randomIndex = Random.Range(0, obstacleTextures.Length - 1);
        return randomIndex >= excludedIndex && excludedIndex >= 0 ? randomIndex + 1 : randomIndex;
    }

    private float GetRightmostObstacleX()
    {
        float rightmostX = TrackWidth;

        foreach (RunnerObstacle obstacle in obstacles)
        {
            if (obstacle.RectTransform.anchoredPosition.x > rightmostX)
            {
                rightmostX = obstacle.RectTransform.anchoredPosition.x;
            }
        }

        return rightmostX;
    }

    private int GetRightmostObstacleTypeIndex()
    {
        float rightmostX = float.MinValue;
        int typeIndex = -1;

        foreach (RunnerObstacle obstacle in obstacles)
        {
            if (obstacle.RectTransform.anchoredPosition.x > rightmostX)
            {
                rightmostX = obstacle.RectTransform.anchoredPosition.x;
                typeIndex = obstacle.TypeIndex;
            }
        }

        return typeIndex;
    }

    private void Fail(string obstacleName)
    {
        finished = true;
        messageText.text = $"Hit the {obstacleName}. Press Battle to try another mini game.";
        context.SetStatus($"Chrome Cat Runner failed\n{context.GetBattleStats()}");
        context.ExitAfterDelay(2f);
    }

    private void Complete()
    {
        finished = true;
        context.ApplySuccess(miniGame);
        context.ExitAfterDelay(2f);
    }
}
