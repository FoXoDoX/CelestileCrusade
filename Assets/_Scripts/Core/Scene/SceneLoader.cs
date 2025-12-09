using UnityEngine.SceneManagement;

namespace My.Scripts.Core.Scene
{
    public static class SceneLoader
    {
        public enum Scene
        {
            MainMenuScene,
            LevelsMenuScene,
            GameScene,
            GameOverScene,
        }

        public static void LoadScene(Scene scene)
        {
            SceneManager.LoadScene(scene.ToString());
        }
    }
}
