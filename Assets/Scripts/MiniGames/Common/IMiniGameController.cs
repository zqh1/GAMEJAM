using UnityEngine;

public interface IMiniGameController
{
    GameObject Start(MiniGameDefinition miniGame, MiniGameContext context);
    void Tick(float deltaTime);
    void Stop();
}
