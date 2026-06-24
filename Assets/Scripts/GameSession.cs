using UnityEngine;

/// <summary>
/// Holds data that must survive between scenes.
/// The raising phase writes stats here, and the later battle scene can read them.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public RaisingStats raisingStats = new RaisingStats();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
