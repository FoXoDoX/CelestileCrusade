using UnityEngine;
using UnityEngine.SceneManagement;

namespace My.Scripts.Core.Scene
{
    /// <summary>
    /// Статический класс для управления загрузкой сцен.
    /// </summary>
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

        /// <summary>
        /// Загружает указанную сцену.
        /// </summary>
        public static void LoadScene(Scene scene)
        {
            string sceneName = scene.ToString();

            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Перезагружает текущую сцену.
        /// </summary>
        public static void ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            Debug.Log($"[SceneLoader] Reloading scene: {currentSceneName}");

            SceneManager.LoadScene(currentSceneName);
        }

        /// <summary>
        /// Возвращает имя текущей активной сцены.
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Проверяет, является ли текущая сцена указанной.
        /// </summary>
        public static bool IsCurrentScene(Scene scene)
        {
            return GetCurrentSceneName() == scene.ToString();
        }

        #endregion
    }
}