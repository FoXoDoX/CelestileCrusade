using My.Scripts.Core.Utility;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace My.Scripts.EventBus
{
    public class EventManager : PersistentSingleton<EventManager>
    {
        // Разные словари для разных сигнатур
        private readonly Dictionary<GameEvents, HashSet<Action>> _handlers0 = new();
        private readonly Dictionary<GameEvents, HashSet<Action<object>>> _handlers1 = new();
        private readonly Dictionary<GameEvents, HashSet<Action<object, object>>> _handlers2 = new();

        private readonly Dictionary<(GameEvents, Delegate), Delegate> _wrapperMap = new();

        #region Add Handlers

        public void AddHandler(GameEvents gameEvent, Action handler)
        {
            if (handler == null) return;
            GetOrCreate(_handlers0, gameEvent).Add(handler);
        }

        public void AddHandler<T>(GameEvents gameEvent, Action<T> handler)
        {
            if (handler == null) return;
            Action<object> wrapper = arg => handler((T)arg);
            _wrapperMap[(gameEvent, handler)] = wrapper;
            GetOrCreate(_handlers1, gameEvent).Add(wrapper);
        }

        public void AddHandler<T1, T2>(GameEvents gameEvent, Action<T1, T2> handler)
        {
            if (handler == null) return;
            Action<object, object> wrapper = (a1, a2) => handler((T1)a1, (T2)a2);
            _wrapperMap[(gameEvent, handler)] = wrapper;
            GetOrCreate(_handlers2, gameEvent).Add(wrapper);
        }

        #endregion

        #region Remove Handlers

        public void RemoveHandler(GameEvents gameEvent, Action handler)
        {
            if (handler == null) return;
            if (_handlers0.TryGetValue(gameEvent, out var set))
                set.Remove(handler);
        }

        public void RemoveHandler<T>(GameEvents gameEvent, Action<T> handler)
        {
            if (handler == null) return;
            var key = (gameEvent, (Delegate)handler);
            if (_wrapperMap.TryGetValue(key, out var wrapper))
            {
                if (_handlers1.TryGetValue(gameEvent, out var set))
                    set.Remove((Action<object>)wrapper);
                _wrapperMap.Remove(key);
            }
        }

        public void RemoveHandler<T1, T2>(GameEvents gameEvent, Action<T1, T2> handler)
        {
            if (handler == null) return;
            var key = (gameEvent, (Delegate)handler);
            if (_wrapperMap.TryGetValue(key, out var wrapper))
            {
                if (_handlers2.TryGetValue(gameEvent, out var set))
                    set.Remove((Action<object, object>)wrapper);
                _wrapperMap.Remove(key);
            }
        }

        #endregion

        #region Broadcast

        public void Broadcast(GameEvents gameEvent)
        {
            if (!_handlers0.TryGetValue(gameEvent, out var handlers)) return;
            foreach (var handler in new List<Action>(handlers))
                SafeInvoke(handler);
        }

        public void Broadcast<T>(GameEvents gameEvent, T arg)
        {
            if (!_handlers1.TryGetValue(gameEvent, out var handlers)) return;
            foreach (var handler in new List<Action<object>>(handlers))
                SafeInvoke(() => handler(arg));
        }

        public void Broadcast<T1, T2>(GameEvents gameEvent, T1 arg1, T2 arg2)
        {
            if (!_handlers2.TryGetValue(gameEvent, out var handlers)) return;
            foreach (var handler in new List<Action<object, object>>(handlers))
                SafeInvoke(() => handler(arg1, arg2));
        }

        #endregion

        #region Utility

        private HashSet<THandler> GetOrCreate<THandler>(
            Dictionary<GameEvents, HashSet<THandler>> dict,
            GameEvents gameEvent)
        {
            if (!dict.TryGetValue(gameEvent, out var set))
            {
                set = new HashSet<THandler>();
                dict[gameEvent] = set;
            }
            return set;
        }

        public void RemoveAllHandlers(GameEvents gameEvent)
        {
            _handlers0.Remove(gameEvent);
            _handlers1.Remove(gameEvent);
            _handlers2.Remove(gameEvent);

            var keysToRemove = new List<(GameEvents, Delegate)>();
            foreach (var key in _wrapperMap.Keys)
                if (key.Item1 == gameEvent)
                    keysToRemove.Add(key);
            foreach (var key in keysToRemove)
                _wrapperMap.Remove(key);
        }

        public void ClearAll()
        {
            _handlers0.Clear();
            _handlers1.Clear();
            _handlers2.Clear();
            _wrapperMap.Clear();
        }

        private void SafeInvoke(Action action)
        {
            try { action(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        #endregion
    }
}