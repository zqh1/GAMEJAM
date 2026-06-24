using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Any raising phase value that a card choice can change.
/// </summary>
public enum RaisingStatType
{
    Body = 0,
    Mind = 1,
    Money = 3,
    Energy = 4
}

/// <summary>
/// One raising stat change made by a choice on a card.
/// For example: Body +2, Mind -1, or Money +3.
/// </summary>
[Serializable]
public class RaisingStatChange
{
    public RaisingStatType statType;
    public int amount;
}

/// <summary>
/// One hidden evaluation answer recorded by a choice on a card.
/// </summary>
[Serializable]
public class HiddenTraitAnswer
{
    public HiddenTraitType traitType;
    public bool isCorrect;
}

/// <summary>
/// One choice on a Reigns-like card. It can change visible stats and record hidden evaluation answers.
/// </summary>
[Serializable]
public class CatChoice
{
    [TextArea]
    public string choiceText;

    public List<RaisingStatChange> statChanges = new List<RaisingStatChange>();
    public List<HiddenTraitAnswer> hiddenTraitAnswers = new List<HiddenTraitAnswer>();
}

/// <summary>
/// A ScriptableObject asset representing one event card in the cat raising phase.
/// Create these from the Unity editor through Assets > Create > Cat Raising > Cat Card.
/// </summary>
[CreateAssetMenu(fileName = "NewCatCard", menuName = "Cat Raising/Cat Card")]
public class CatCard : ScriptableObject
{
    [TextArea]
    public string eventText;

    public Sprite catSprite;

    public CatChoice leftChoice = new CatChoice();
    public CatChoice rightChoice = new CatChoice();
}
