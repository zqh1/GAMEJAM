using UnityEngine;

[CreateAssetMenu(fileName = "NewMiniGame", menuName = "Cat Battle/Mini Game")]
public class MiniGameDefinition : ScriptableObject
{
    public string displayName;

    [TextArea]
    public string description;

    public int bossHpDamage;
    public int dominanceGain;
}
