using UnityEngine;

// Creates the whole game at startup, so the scene can stay empty.
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        new GameObject("Game").AddComponent<GameManager>();
    }
}
