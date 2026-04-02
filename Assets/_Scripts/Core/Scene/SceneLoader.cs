using UnityEngine;
using UnityEngine.SceneManagement;

namespace My.Scripts.Core.Scene
{
    public static class SceneLoader
    {
        #region Enums

        public enum Scene
        {
            MainMenuScene,
            LevelsMenuScene,
            GameScene,
            GameOverScene
        }

        #endregion

        #region Public Methods

        public static void LoadScene(Scene scene,
            TransitionDirection direction = TransitionDirection.Right)
        {
            string sceneName = scene.ToString();
            TransitionType type = GetTransitionType(scene);

            Debug.Log($"[SceneLoader] Loading scene: {sceneName} ({type}, {direction})");

            if (SceneTransitionManager.HasInstance)
            {
                SceneTransitionManager.Instance.TransitionToScene(sceneName, type, direction);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public static void ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            Debug.Log($"[SceneLoader] Reloading scene: {currentSceneName}");

            if (SceneTransitionManager.HasInstance)
            {
                SceneTransitionManager.Instance.TransitionToScene(
                    currentSceneName, TransitionType.Circle);
            }
            else
            {
                SceneManager.LoadScene(currentSceneName);
            }
        }

        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        public static bool IsCurrentScene(Scene scene)
        {
            return GetCurrentSceneName() == scene.ToString();
        }

        #endregion

        #region Private Methods

        private static TransitionType GetTransitionType(Scene scene)
        {
            string currentScene = GetCurrentSceneName();

            if (currentScene == Scene.GameScene.ToString())
                return TransitionType.Circle;

            if (scene == Scene.GameScene)
                return TransitionType.Circle;

            return TransitionType.Strips;
        }

        #endregion
    }
}