using UnityEngine;

namespace My.Scripts.Core.Utility
{
    /// <summary>
    /// Базовый класс — просто хранит статическую ссылку, без защиты от дубликатов.
    /// Используется, когда гарантированно один экземпляр (например, через префаб).
    /// </summary>
    public abstract class StaticInstance<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        /// <summary>
        /// True, если синглтон существует и не уничтожен.
        /// Безопасная проверка без побочных эффектов.
        /// </summary>
        public static bool HasInstance => Instance != null;

        protected virtual void Awake()
        {
            Instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>
    /// Синглтон с защитой от дубликатов — лишние экземпляры уничтожаются.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                // Ленивый поиск
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (!InitializeSingleton())
            {
                return; // Объект будет уничтожен
            }

            OnSingletonAwake();
        }

        /// <summary>
        /// Вызывается только на "победившем" экземпляре синглтона.
        /// Переопредели вместо Awake для инициализации.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <returns>True если это главный экземпляр, false если дубликат</returns>
        private bool InitializeSingleton()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(
                    $"[Singleton] Duplicate {typeof(T).Name} on '{gameObject.name}'. " +
                    $"Keeping '{_instance.gameObject.name}', destroying this.",
                    gameObject
                );
                Destroy(gameObject);
                return false;
            }

            _instance = this as T;
            return true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Глобальный флаг выхода из приложения.
    /// </summary>
    public static class ApplicationState
    {
        public static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            IsQuitting = false;
            Application.quitting += () => IsQuitting = true;
        }
    }

    /// <summary>
    /// Синглтон, существующий после смены сцены (то есть постоянно).
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                // Если выходим из приложения — не создаём и не ищем
                if (ApplicationState.IsQuitting)
                {
                    return null;  // Без предупреждения!
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null && !ApplicationState.IsQuitting;

        protected virtual void Awake()
        {
            if (!InitializeSingleton())
            {
                return;
            }

            DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }

        protected virtual void OnSingletonAwake() { }

        private bool InitializeSingleton()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(
                    $"[PersistentSingleton] Duplicate {typeof(T).Name} detected. Destroying.",
                    gameObject
                );
                Destroy(gameObject);
                return false;
            }

            _instance = this as T;
            return true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Приватный синглтон — без публичного доступа к Instance.
    /// Для систем, которые работают автономно и не должны вызываться извне.
    /// </summary>
    public abstract class PrivateSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        protected static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            OnSingletonAwake();
        }

        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    /// <summary>
    /// Приватный синглтон + DontDestroyOnLoad.
    /// </summary>
    public abstract class PrivatePersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        protected static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnSingletonAwake();
        }

        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}