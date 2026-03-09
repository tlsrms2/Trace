using UnityEngine;
using UnityEngine.SceneManagement; // 필요

public class SceneChanger : MonoBehaviour
{
    public void ToGameScene()
    {
        SceneManager.LoadScene("GameScene"); 
    }
}