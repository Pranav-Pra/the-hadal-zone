using UnityEngine;
using UnityEngine.SceneManagement;

public class SkipIntro : MonoBehaviour
{
    public string gameplayScene = "Hadal";

    public void Skip()
    {
        SceneManager.LoadScene(gameplayScene);
    }
}