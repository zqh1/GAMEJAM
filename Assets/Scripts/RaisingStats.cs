using System;
using UnityEngine;

/// <summary>
/// Hidden traits the cat uses to evaluate the player.
/// </summary>
public enum HiddenTraitType
{
    Affection,
    Obedience,
    Generosity,
    Tolerance,
    Hardworking
}

/// <summary>
/// Stores one hidden evaluation score as a fraction.
/// </summary>
[Serializable]
public class HiddenTraitScore
{
    public int numerator = 1;
    public int denominator = 2;

    public void RecordAnswer(bool isCorrect)
    {
        denominator = Mathf.Max(1, denominator);

        if (isCorrect)
        {
            numerator++;
        }

        denominator++;
    }

    public int GetFinalScore()
    {
        int safeDenominator = Mathf.Max(1, denominator);
        float score = (float)numerator / safeDenominator * 100f;
        score *= UnityEngine.Random.Range(0.95f, 1.05f);

        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }

    public int GetRawPercent()
    {
        int safeDenominator = Mathf.Max(1, denominator);
        float score = (float)numerator / safeDenominator * 100f;

        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }
}

/// <summary>
/// Stores visible raising stats and hidden cat evaluation traits.
/// </summary>
[Serializable]
public class RaisingStats
{
    public int body = 5;
    public int mind = 5;
    public int money = 6;
    public int energy = 6;

    public HiddenTraitScore affection = new HiddenTraitScore();
    public HiddenTraitScore obedience = new HiddenTraitScore();
    public HiddenTraitScore generosity = new HiddenTraitScore();
    public HiddenTraitScore tolerance = new HiddenTraitScore();
    public HiddenTraitScore hardworking = new HiddenTraitScore();

    /// <summary>
    /// Keeps every visible raising value inside the expected 0 to 10 range.
    /// Call this after applying card choice changes.
    /// </summary>
    public void Clamp()
    {
        body = Mathf.Clamp(body, 0, 10);
        mind = Mathf.Clamp(mind, 0, 10);
        money = Mathf.Clamp(money, 0, 10);
        energy = Mathf.Clamp(energy, 0, 10);
    }

    public void RecordHiddenAnswer(HiddenTraitType traitType, bool isCorrect)
    {
        GetHiddenTraitScore(traitType).RecordAnswer(isCorrect);
    }

    public int GetHiddenTraitFinalScore(HiddenTraitType traitType)
    {
        return GetHiddenTraitScore(traitType).GetFinalScore();
    }

    public string GetHiddenTraitDebugText()
    {
        HiddenTraitScore affectionScore = GetHiddenTraitScore(HiddenTraitType.Affection);
        HiddenTraitScore obedienceScore = GetHiddenTraitScore(HiddenTraitType.Obedience);
        HiddenTraitScore generosityScore = GetHiddenTraitScore(HiddenTraitType.Generosity);
        HiddenTraitScore toleranceScore = GetHiddenTraitScore(HiddenTraitType.Tolerance);
        HiddenTraitScore hardworkingScore = GetHiddenTraitScore(HiddenTraitType.Hardworking);

        return
            "Hidden Traits Debug\n" +
            FormatHiddenTraitDebugLine("Affection", affectionScore) + "\n" +
            FormatHiddenTraitDebugLine("Obedience", obedienceScore) + "\n" +
            FormatHiddenTraitDebugLine("Generosity", generosityScore) + "\n" +
            FormatHiddenTraitDebugLine("Tolerance", toleranceScore) + "\n" +
            FormatHiddenTraitDebugLine("Hardworking", hardworkingScore);
    }

    private HiddenTraitScore GetHiddenTraitScore(HiddenTraitType traitType)
    {
        switch (traitType)
        {
            case HiddenTraitType.Affection:
                return EnsureHiddenTraitScore(ref affection);
            case HiddenTraitType.Obedience:
                return EnsureHiddenTraitScore(ref obedience);
            case HiddenTraitType.Generosity:
                return EnsureHiddenTraitScore(ref generosity);
            case HiddenTraitType.Tolerance:
                return EnsureHiddenTraitScore(ref tolerance);
            case HiddenTraitType.Hardworking:
                return EnsureHiddenTraitScore(ref hardworking);
            default:
                Debug.LogWarning($"Unhandled hidden trait type: {traitType}");
                return EnsureHiddenTraitScore(ref affection);
        }
    }

    private HiddenTraitScore EnsureHiddenTraitScore(ref HiddenTraitScore score)
    {
        if (score == null)
        {
            score = new HiddenTraitScore();
        }

        return score;
    }

    private string FormatHiddenTraitDebugLine(string label, HiddenTraitScore score)
    {
        int safeDenominator = Mathf.Max(1, score.denominator);
        return $"{label}: {score.GetRawPercent()}";
    }
}
