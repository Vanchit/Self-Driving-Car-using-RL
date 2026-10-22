using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayButtonController : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "RL_Scene";

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayGame);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
