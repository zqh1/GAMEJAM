using UnityEngine;

public class InstantMiniGameController : IMiniGameController
{
    public GameObject Start(MiniGameDefinition miniGame, MiniGameContext context)
    {
        context.ApplySuccess(miniGame);
        return null;
    }

    public void Tick(float deltaTime)
    {
    }

    public void Stop()
    {
    }
}
