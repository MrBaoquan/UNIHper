using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using DNHper;
using UnityEngine;

namespace UNIHper
{
    public abstract class UEvent { }

    public interface IEventHandler
    {
        void SubscribeEvents();
        void UnsubscribeEvents();
    }

    /// <summary>
    /// 事件管理模块
    /// </summary>
    public class EventManager : Singleton<EventManager>
    {
        /// <summary>
        /// 根据事件类型进行分组
        /// </summary>
        /// <returns></returns>
        private Dictionary<Type, Action<UEvent>> delegates = new Dictionary<Type, Action<UEvent>>();

        /// <summary>
        /// 回调原型映射  根据原型查找真实回调
        /// </summary>
        /// <returns></returns>
        private Dictionary<Delegate, Action<UEvent>> lookup =
            new Dictionary<Delegate, Action<UEvent>>();

        public void Register<T>(Action<T> InDelegate)
            where T : UEvent
        {
            if (lookup.ContainsKey(InDelegate))
            {
                Debug.LogWarning("has been registered already");
                return;
            }
            Action<UEvent> _newDelegate = _event => InDelegate(_event as T);
            lookup.Add(InDelegate, _newDelegate);

            Type _actionKey = typeof(T);
            Action<UEvent> _delegate;
            if (delegates.TryGetValue(_actionKey, out _delegate))
            {
                _delegate += _newDelegate;
                delegates[_actionKey] = _delegate;
            }
            else
            {
                delegates.Add(_actionKey, _newDelegate);
            }
        }

        public void Unregister<T>(Action<T> InDelegate)
            where T : UEvent
        {
            Action<UEvent> _internal_action;
            if (lookup.TryGetValue(InDelegate, out _internal_action))
            {
                Action<UEvent> _delegate;
                Type _actionKey = typeof(T);
                if (delegates.TryGetValue(_actionKey, out _delegate))
                {
                    _delegate -= _internal_action;
                    if (_delegate == null)
                    {
                        delegates.Remove(_actionKey);
                    }
                    else
                    {
                        delegates[_actionKey] = _delegate;
                    }
                }
                lookup.Remove(InDelegate);
            }
        }

        public void Unregister<T>()
            where T : UEvent
        {
            Action<UEvent> _delegate;
            Type _actionKey = typeof(T);
            if (delegates.TryGetValue(_actionKey, out _delegate))
            {
                delegates.Remove(_actionKey);
                var _delegates = _delegate.GetInvocationList().ToList();
                var _internal_keys = lookup
                    .Where(_item => _delegates.Contains(_item.Value))
                    .ToDictionary(_ => _.Key, _ => _.Value)
                    .Keys.ToList();

                _internal_keys.ForEach(_ =>
                {
                    lookup.Remove(_);
                });
            }
        }

        public void Fire(UEvent InEvent)
        {
            Action<UEvent> _internal_action;
            if (delegates.TryGetValue(InEvent.GetType(), out _internal_action))
            {
                _internal_action.Invoke(InEvent);
            }
        }
    }
}
