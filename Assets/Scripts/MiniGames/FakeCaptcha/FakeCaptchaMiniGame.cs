using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FakeCaptchaMiniGame : IMiniGameController
{
    private MiniGameDefinition miniGame;
    private MiniGameContext context;
    private RawImage tauntingCatImage;
    private TMP_Text tauntText;
    private bool finished;

    public GameObject Start(MiniGameDefinition miniGame, MiniGameContext context)
    {
        this.miniGame = miniGame;
        this.context = context;

        context.SetStatus("Fake CAPTCHA\nFind the real way through.");

        GameObject panel = new GameObject("FakeCaptchaPanel", typeof(RectTransform));
        panel.transform.SetParent(context.CanvasRoot, false);

        RectTransform rootRect = panel.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image outerCard = MiniGameUi.CreatePanel(panel.transform, "CardBorder", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.05f, 0.07f, 1f));
        MiniGameUi.SetRect(outerCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 260f));

        Image innerCard = MiniGameUi.CreatePanel(outerCard.transform, "Card", Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f), new Color(0.89f, 0.9f, 1f, 1f));

        Button closeButton = MiniGameUi.CreateButton(innerCard.transform, "HiddenCloseButton", "x", new Vector2(24f, 24f), 15f, context.ProjectPixelFont);
        MiniGameUi.SetRect((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(24f, 24f));
        closeButton.GetComponent<Image>().color = new Color(0.89f, 0.9f, 1f, 0.05f);
        closeButton.onClick.AddListener(Succeed);

        TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>();
        closeLabel.color = new Color(0.05f, 0.05f, 0.07f, 0.22f);

        Button checkboxButton = MiniGameUi.CreateButton(innerCard.transform, "CheckboxButton", string.Empty, new Vector2(88f, 88f), 1f, context.ProjectPixelFont);
        MiniGameUi.SetRect((RectTransform)checkboxButton.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, 0f), new Vector2(88f, 88f));
        checkboxButton.GetComponent<Image>().color = new Color(0.78f, 0.8f, 1f, 1f);
        checkboxButton.onClick.AddListener(Fail);

        Image checkboxInset = MiniGameUi.CreatePanel(checkboxButton.transform, "CheckboxInset", Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f), new Color(0.88f, 0.89f, 1f, 1f));
        checkboxInset.raycastTarget = false;

        GameObject tauntingCatObject = new GameObject("TauntingCat", typeof(RectTransform), typeof(RawImage));
        tauntingCatObject.transform.SetParent(checkboxButton.transform, false);
        MiniGameUi.SetRect((RectTransform)tauntingCatObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        tauntingCatImage = tauntingCatObject.GetComponent<RawImage>();
        tauntingCatImage.texture = context.FakeCaptchaTauntingCatTexture;
        tauntingCatImage.color = Color.white;
        tauntingCatImage.raycastTarget = false;
        tauntingCatObject.SetActive(false);

        TMP_Text prompt = MiniGameUi.CreateText(innerCard.transform, "Prompt", "I'm not a human", 52f, TextAlignmentOptions.Left, Color.black, context.ProjectPixelFont);
        MiniGameUi.SetRect(prompt.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(220f, 0f), new Vector2(-500f, 120f));

        tauntText = MiniGameUi.CreateText(innerCard.transform, "TauntText", string.Empty, 34f, TextAlignmentOptions.Center, new Color(0.72f, 0.06f, 0.12f, 1f), context.ProjectPixelFont);
        MiniGameUi.SetRect(tauntText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(640f, 52f));

        TMP_Text logo = MiniGameUi.CreateText(innerCard.transform, "VerifyLogo", "VERIFY", 34f, TextAlignmentOptions.Center, new Color(0.28f, 0.23f, 0.95f, 1f), context.ProjectPixelFont);
        MiniGameUi.SetRect(logo.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-135f, 0f), new Vector2(190f, 90f));

        return panel;
    }

    public void Tick(float deltaTime)
    {
    }

    public void Stop()
    {
    }

    private void Succeed()
    {
        if (finished)
        {
            return;
        }

        finished = true;
        context.ApplySuccess(miniGame);
        context.ExitAfterDelay(2f);
    }

    private void Fail()
    {
        if (finished)
        {
            return;
        }

        finished = true;
        ShowTaunt();
        context.SetStatus($"Fake CAPTCHA failed\n{context.GetBattleStats()}");
        context.ExitAfterDelay(2f);
    }

    private void ShowTaunt()
    {
        if (tauntingCatImage != null && context.FakeCaptchaTauntingCatTexture != null)
        {
            tauntingCatImage.gameObject.SetActive(true);
        }

        if (tauntText != null)
        {
            tauntText.text = "Can you please look carefully, silly?";
        }
    }
}
