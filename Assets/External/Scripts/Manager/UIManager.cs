using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject pauseMenu;

    void Awake()
    {
        Instance = this;
    }

    public void ShowPause()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
    }

    public void HidePause()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    // 돌아가기 버튼
    public void ResumeGame()
    {
        GameManager.Instance.ResumeGame();
    }

    // 재시작 버튼
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 메인메뉴 버튼
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    // 타이틀에서 게임 시작
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // 게임 종료
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}