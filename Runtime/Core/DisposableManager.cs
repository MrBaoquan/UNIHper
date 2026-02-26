using System;
using System.Collections.Generic;
using DNHper;
using UniRx;

namespace UNIHper
{
    /// <summary>
    /// 通过字符串 Key 管理 IDisposable 资源
    /// 支持两种模式：
    /// 1. 替换模式 (Serial) - 同 key 自动取消旧订阅，只保留最新的
    /// 2. 累积模式 (Composite) - 同 key 累积多个订阅
    /// </summary>
    public class DisposableManager : Singleton<DisposableManager>
    {
        // 替换模式存储 - SerialDisposable 行为
        private readonly Dictionary<string, SerialDisposable> _serialDisposables = new Dictionary<string, SerialDisposable>();

        // 累积模式存储 - CompositeDisposable 行为
        private readonly Dictionary<string, CompositeDisposable> _compositeDisposables = new Dictionary<string, CompositeDisposable>();

        #region 替换模式 (Serial Mode)

        /// <summary>
        /// 设置指定 key 的 IDisposable（替换模式）
        /// 如果该 key 已存在订阅，会自动取消旧订阅
        /// </summary>
        public void Set(string key, IDisposable disposable)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (!_serialDisposables.TryGetValue(key, out var serial))
            {
                serial = new SerialDisposable();
                _serialDisposables[key] = serial;
            }
            serial.Disposable = disposable;
        }

        /// <summary>
        /// 取消指定 key 的订阅（替换模式）
        /// </summary>
        public void Cancel(string key)
        {
            if (_serialDisposables.TryGetValue(key, out var serial))
            {
                serial.Disposable = null;
            }
        }

        /// <summary>
        /// 检查指定 key 是否有活跃的订阅（替换模式）
        /// </summary>
        public bool HasSerial(string key)
        {
            return _serialDisposables.TryGetValue(key, out var serial) && serial.Disposable != null;
        }

        #endregion

        #region 累积模式 (Composite Mode)

        /// <summary>
        /// 获取或创建指定 key 的 CompositeDisposable（累积模式）
        /// </summary>
        public CompositeDisposable GetComposite(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (!_compositeDisposables.TryGetValue(key, out var composite))
            {
                composite = new CompositeDisposable();
                _compositeDisposables[key] = composite;
            }
            return composite;
        }

        /// <summary>
        /// 添加 IDisposable 到指定 key（累积模式）
        /// </summary>
        public void Add(string key, IDisposable disposable)
        {
            GetComposite(key).Add(disposable);
        }

        /// <summary>
        /// 释放并清除指定 key 的所有 IDisposable（累积模式）
        /// </summary>
        public void Dispose(string key)
        {
            if (_compositeDisposables.TryGetValue(key, out var composite))
            {
                composite.Dispose();
                _compositeDisposables.Remove(key);
            }
        }

        /// <summary>
        /// 清除指定 key 但不调用 Dispose（累积模式）
        /// </summary>
        public void Clear(string key)
        {
            if (_compositeDisposables.TryGetValue(key, out var composite))
            {
                composite.Clear();
            }
        }

        /// <summary>
        /// 检查指定 key 是否存在（累积模式）
        /// </summary>
        public bool HasComposite(string key)
        {
            return _compositeDisposables.TryGetValue(key, out var c) && c.Count > 0;
        }

        /// <summary>
        /// 获取指定 key 的 IDisposable 数量（累积模式）
        /// </summary>
        public int Count(string key)
        {
            return _compositeDisposables.TryGetValue(key, out var c) ? c.Count : 0;
        }

        #endregion

        #region 全局操作

        /// <summary>
        /// 释放所有 key 的所有 IDisposable
        /// </summary>
        public void DisposeAll()
        {
            // 清理替换模式
            foreach (var kvp in _serialDisposables)
            {
                kvp.Value.Dispose();
            }
            _serialDisposables.Clear();

            // 清理累积模式
            foreach (var kvp in _compositeDisposables)
            {
                kvp.Value.Dispose();
            }
            _compositeDisposables.Clear();
        }

        /// <summary>
        /// 取消所有替换模式的订阅
        /// </summary>
        public void CancelAll()
        {
            foreach (var kvp in _serialDisposables)
            {
                kvp.Value.Disposable = null;
            }
        }

        #endregion
    }
}
