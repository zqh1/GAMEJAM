using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaisingManager : MonoBehaviour
{
    [Header("Cards")]
    public List<CatCard> allCards = new List<CatCard>();
    public int totalMonths = 12;
    public float extraEventChance = 0.3f;

    [Header("UI")]
    public TMP_Text dialogueText;
    public TMP_Text turnText;
    public TMP_Text bodyText;
    public TMP_Text mindText;
    public TMP_Text moneyText;
    public TMP_Text energyText;
    public bool showHiddenDebugUI = true;
    public TMP_Text hiddenDebugText;
    public Image catImage;
    public bool showBattleShortcut = true;
    public string battleSceneName = "BattleScene";

    private class ScheduledCardEvent
    {
        public CatCard card;
        public int monthIndex;
        public int day;
    }

    private readonly string[] monthNames =
    {
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    };

    private readonly int[] monthLengths =
    {
        31, 28, 31, 30, 31, 30,
        31, 31, 30, 31, 30, 31
    };

    private readonly List<CatCard> deck = new List<CatCard>();
    private readonly List<ScheduledCardEvent> currentMonthEvents = new List<ScheduledCardEvent>();
    private CatCard currentCard;
    private ScheduledCardEvent currentScheduledEvent;
    private int currentMonthIndex;
    private bool isWaitingForNextCard;

    public bool IsWaitingForNextCard => isWaitingForNextCard;

    private void Start()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogWarning("RaisingManager could not find a GameSession. Add a GameSession object to the first scene.");
        }

        deck.Clear();

        if (allCards != null)
        {
            deck.AddRange(allCards);
        }
        else
        {
            Debug.LogWarning("RaisingManager has no allCards list assigned.");
        }

        Random.InitState(System.Environment.TickCount);
        ShuffleDeck();
        LogShuffledDeckOrder();

        if (deck.Count < totalMonths)
        {
            Debug.LogWarning($"RaisingManager has {deck.Count} cards, but the calendar is set to {totalMonths} months. The raising phase may end before December.");
        }

        currentMonthIndex = 0;
        isWaitingForNextCard = false;

        UpdateStatsUI();
        UpdateHiddenDebugUI();
        CreateBattleShortcutButton();
        DrawNextCard();
    }

    // Draws the next scheduled event, prepares a new month, or loads the battle scene when raising is finished.
    public void DrawNextCard()
    {
        while (currentMonthEvents.Count == 0)
        {
            if (currentMonthIndex >= totalMonths)
            {
                LoadBattleScene();
                return;
            }

            if (deck.Count == 0)
            {
                Debug.LogWarning("The card deck became empty before December. Loading BattleScene early.");
                LoadBattleScene();
                return;
            }

            PrepareCurrentMonth();
        }

        currentScheduledEvent = currentMonthEvents[0];
        currentMonthEvents.RemoveAt(0);
        currentCard = currentScheduledEvent.card;
        isWaitingForNextCard = false;

        UpdateStatsUI();
        ShowCurrentCard();

        if (currentMonthEvents.Count == 0)
        {
            currentMonthIndex++;
        }
    }


    // Applies the left choice. SwipeCard calls this after a left swipe.
    public void ChooseLeft()
    {
        if (currentCard == null)
        {
            Debug.LogWarning("ChooseLeft was called, but there is no current card.");
            return;
        }

        ApplyChoice(currentCard.leftChoice);
    }


    // Applies the right choice. SwipeCard calls this after a right swipe.
    public void ChooseRight()
    {
        if (currentCard == null)
        {
            Debug.LogWarning("ChooseRight was called, but there is no current card.");
            return;
        }

        ApplyChoice(currentCard.rightChoice);
    }

    private void ShowCurrentCard()
    {
        if (currentCard == null)
        {
            Debug.LogWarning("Tried to show a card, but currentCard is null.");
            return;
        }

        ResetChoicePreview();

        if (turnText != null)
        {
            turnText.text = GetCurrentDateText();
        }

        if (catImage != null)
        {
            catImage.sprite = currentCard.catSprite;
            catImage.enabled = currentCard.catSprite != null;
        }
        else
        {
            Debug.LogWarning("RaisingManager is missing a catImage reference.");
        }
    }

    private void ApplyChoice(CatChoice choice)
    {
        if (isWaitingForNextCard)
        {
            return;
        }

        if (choice == null)
        {
            Debug.LogWarning("Tried to apply a null choice.");
            return;
        }

        isWaitingForNextCard = true;

        if (GameSession.Instance != null)
        {
            if (GameSession.Instance.raisingStats == null)
            {
                GameSession.Instance.raisingStats = new RaisingStats();
                Debug.LogWarning("GameSession had no RaisingStats instance, so a new one was created.");
            }

            ApplyRaisingStatChanges(GameSession.Instance.raisingStats, choice.statChanges);
            ApplyHiddenTraitAnswers(GameSession.Instance.raisingStats, choice.hiddenTraitAnswers);
            GameSession.Instance.raisingStats.Clamp();
        }
        else
        {
            Debug.LogWarning("Choice was selected, but no GameSession exists to store raising stats.");
        }

        UpdateStatsUI();
        StartCoroutine(WaitThenDrawNextCard());
    }

    // Shows the current choice being previewed by card dragging.
    public void PreviewChoice(int direction)
    {
        if (currentCard == null || isWaitingForNextCard)
        {
            return;
        }

        // Direction less than 0 previews left, greater than 0 previews right, and 0 clears the preview.
        if (direction < 0)
        {
            SetText(dialogueText, GetChoiceText(currentCard.leftChoice), "dialogueText");
        }
        else if (direction > 0)
        {
            SetText(dialogueText, GetChoiceText(currentCard.rightChoice), "dialogueText");
        }
        else
        {
            ResetChoicePreview();
        }
    }


    // Shows only the current card event text.
    public void ResetChoicePreview()
    {
        if (currentCard == null)
        {
            return;
        }

        SetText(dialogueText, currentCard.eventText, "dialogueText");
    }

    private void PrepareCurrentMonth()
    {
        currentMonthEvents.Clear();

        int clampedMonthIndex = Mathf.Clamp(currentMonthIndex, 0, monthLengths.Length - 1);
        int daysInMonth = monthLengths[clampedMonthIndex];
        int remainingMonthsAfterThisMonth = Mathf.Max(0, totalMonths - currentMonthIndex - 1);

        CatCard mandatoryCard = DrawCardFromDeck();
        if (mandatoryCard == null)
        {
            Debug.LogWarning($"{GetMonthName(currentMonthIndex)} could not schedule a mandatory event because no valid cards were available.");
            return;
        }

        List<CatCard> cardsForMonth = new List<CatCard> { mandatoryCard };

        bool hasEnoughExtraCards = deck.Count > remainingMonthsAfterThisMonth;
        bool shouldDrawExtraCard = Random.value < extraEventChance;

        if (hasEnoughExtraCards && shouldDrawExtraCard)
        {
            CatCard extraCard = DrawCardFromDeck();
            if (extraCard != null)
            {
                cardsForMonth.Add(extraCard);
            }
        }

        List<int> days = GenerateEventDays(daysInMonth, cardsForMonth.Count);

        for (int i = 0; i < cardsForMonth.Count; i++)
        {
            ScheduledCardEvent scheduledEvent = new ScheduledCardEvent
            {
                card = cardsForMonth[i],
                monthIndex = currentMonthIndex,
                day = days[i]
            };

            currentMonthEvents.Add(scheduledEvent);
        }

        LogScheduledMonthEvents();
    }

    private CatCard DrawCardFromDeck()
    {
        while (deck.Count > 0)
        {
            CatCard card = deck[0];
            deck.RemoveAt(0);

            if (card != null)
            {
                return card;
            }

            Debug.LogWarning("Skipped a missing card while drawing from the deck.");
        }

        return null;
    }

    private List<int> GenerateEventDays(int daysInMonth, int eventCount)
    {
        List<int> days = new List<int>();

        if (eventCount <= 0)
        {
            return days;
        }

        int safeDaysInMonth = Mathf.Max(1, daysInMonth);

        if (eventCount == 1 || safeDaysInMonth == 1)
        {
            days.Add(Random.Range(1, safeDaysInMonth + 1));
            return days;
        }

        int firstDay = Random.Range(1, safeDaysInMonth + 1);
        int secondDay = Random.Range(1, safeDaysInMonth + 1);

        while (secondDay == firstDay && safeDaysInMonth > 1)
        {
            secondDay = Random.Range(1, safeDaysInMonth + 1);
        }

        days.Add(Mathf.Min(firstDay, secondDay));
        days.Add(Mathf.Max(firstDay, secondDay));
        return days;
    }

    private void LogScheduledMonthEvents()
    {
        List<string> eventDescriptions = new List<string>();

        foreach (ScheduledCardEvent scheduledEvent in currentMonthEvents)
        {
            string cardName = scheduledEvent.card != null ? scheduledEvent.card.name : "(Missing Card)";
            eventDescriptions.Add($"{GetMonthName(scheduledEvent.monthIndex)} {scheduledEvent.day}: {cardName}");
        }

        Debug.Log($"Scheduled {GetMonthName(currentMonthIndex)} events: {string.Join(", ", eventDescriptions.ToArray())}");
    }

    private void ApplyRaisingStatChanges(RaisingStats stats, List<RaisingStatChange> changes)
    {
        if (stats == null)
        {
            Debug.LogWarning("Tried to apply raising stat changes, but RaisingStats is null.");
            return;
        }

        if (changes == null)
        {
            return;
        }

        foreach (RaisingStatChange change in changes)
        {
            if (change == null)
            {
                continue;
            }

            switch (change.statType)
            {
                case RaisingStatType.Body:
                    stats.body += change.amount;
                    break;
                case RaisingStatType.Mind:
                    stats.mind += change.amount;
                    break;
                case RaisingStatType.Money:
                    stats.money += change.amount;
                    break;
                case RaisingStatType.Energy:
                    stats.energy += change.amount;
                    break;
                default:
                    Debug.LogWarning($"Unhandled raising stat type: {change.statType}");
                    break;
            }
        }
    }

    private void ApplyHiddenTraitAnswers(RaisingStats stats, List<HiddenTraitAnswer> answers)
    {
        if (stats == null)
        {
            Debug.LogWarning("Tried to apply hidden trait answers, but RaisingStats is null.");
            return;
        }

        if (answers == null)
        {
            return;
        }

        foreach (HiddenTraitAnswer answer in answers)
        {
            if (answer == null)
            {
                continue;
            }

            stats.RecordHiddenAnswer(answer.traitType, answer.isCorrect);
        }
    }


    // Refreshes the visible stat/resource labels.
    public void UpdateStatsUI()
    {
        if (GameSession.Instance == null)
        {
            Debug.LogWarning("Cannot update stats UI because there is no GameSession.");
            return;
        }

        if (GameSession.Instance.raisingStats == null)
        {
            GameSession.Instance.raisingStats = new RaisingStats();
            Debug.LogWarning("GameSession had no RaisingStats instance, so a new one was created.");
        }

        RaisingStats stats = GameSession.Instance.raisingStats;

        SetText(bodyText, $"Body: {new string('|', stats.body)}", "bodyText");
        SetText(mindText, $"Mind: {new string('|', stats.mind)}", "mindText");
        SetText(moneyText, $"Money: {new string('|', stats.money)}", "moneyText");
        SetText(energyText, $"Energy: {new string('|', stats.energy)}", "energyText");
        UpdateHiddenDebugUI();
    }

    private void UpdateHiddenDebugUI()
    {
        if (hiddenDebugText == null)
        {
            return;
        }

        if (!showHiddenDebugUI)
        {
            hiddenDebugText.text = string.Empty;
            return;
        }

        if (GameSession.Instance == null || GameSession.Instance.raisingStats == null)
        {
            return;
        }

        hiddenDebugText.text = GameSession.Instance.raisingStats.GetHiddenTraitDebugText();
    }

    private IEnumerator WaitThenDrawNextCard()
    {
        yield return new WaitForSeconds(0.4f);
        DrawNextCard();
    }

    private void ShuffleDeck()
    {
        // Fisher-Yates shuffle: walk backward and swap each card with a random earlier card.
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CatCard temporaryCard = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temporaryCard;
        }
    }

    private void LogShuffledDeckOrder()
    {
        List<string> cardNames = new List<string>();

        foreach (CatCard card in deck)
        {
            cardNames.Add(card != null ? card.name : "(Missing Card)");
        }

        Debug.Log("Shuffled card order: " + string.Join(", ", cardNames.ToArray()));
    }

    private string GetCurrentDateText()
    {
        if (currentScheduledEvent == null)
        {
            return string.Empty;
        }

        return $"{currentScheduledEvent.day} {GetMonthName(currentScheduledEvent.monthIndex)}";
    }

    private string GetMonthName(int monthIndex)
    {
        if (monthIndex >= 0 && monthIndex < monthNames.Length)
        {
            return monthNames[monthIndex];
        }

        return $"Month {monthIndex + 1}";
    }

    private void LoadBattleScene()
    {
        OpenBattleScene();
    }

    public void OpenBattleScene()
    {
        SceneManager.LoadScene(battleSceneName);
    }

    private void CreateBattleShortcutButton()
    {
        if (!showBattleShortcut)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("RaisingManager could not create the battle button because no Canvas was found.");
            return;
        }

        if (canvas.transform.Find("BattleShortcutButton") != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject("BattleShortcutButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-18f, -18f);
        buttonRect.sizeDelta = new Vector2(110f, 42f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.24f, 0.32f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(OpenBattleScene);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Battle";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }

    private void SetText(TMP_Text textField, string value, string fieldName)
    {
        if (textField == null)
        {
            Debug.LogWarning($"RaisingManager is missing a {fieldName} reference.");
            return;
        }

        textField.text = value;
    }

    private string GetChoiceText(CatChoice choice)
    {
        if (choice == null)
        {
            Debug.LogWarning("Current card has a missing choice.");
            return string.Empty;
        }

        return choice.choiceText;
    }
}
