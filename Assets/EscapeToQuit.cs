using UnityEngine;

public sealed class EscapeToQuit : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var shortcut = new GameObject(nameof(EscapeToQuit));
        shortcut.AddComponent<EscapeToQuit>();
        DontDestroyOnLoad(shortcut);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
