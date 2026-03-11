using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject pauseMenu;

    [Header("Leaderboard UI")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateLeaderboardUI();
    }

    public void UpdateLeaderboardUI()
    {
        var leaderboard = LeaderboardManager.Instance.GetLeaderboard();

        rankText.text = "";
        nameText.text = "";
        timeText.text = "";

        foreach (var entry in leaderboard)
        {
            rankText.text += entry.rank + "\n";
            nameText.text += entry.playerName + "\n";
            timeText.text += entry.clearTime.ToString("F2") + "\n";
        }
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

    public void ResumeGame()
    {
        GameManager.Instance.ResumeGame();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}