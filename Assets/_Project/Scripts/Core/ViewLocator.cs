using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Görünüm (view) kaydı. UI view'ları OnEnable'da kendini arayüz tipiyle kaydeder;
    /// denetleyiciler Get&lt;IScaleView&gt;() ile bulur. Böylece denetleyici hiçbir Canvas'a ya da objeye bağlı olmaz.
    /// Aynı arayüzden birden fazla view gerekiyorsa (iki terazi gibi) denetleyicinin kendi alanı kullanılır.
    /// </summary>
    public static class ViewLocator
    {
        private static readonly Dictionary<Type, object> Views = new Dictionary<Type, object>();

        /// <summary>Kayıt değişince (view açıldı/kapandı) tetiklenir. Denetleyiciler önbelleğini yeniler.</summary>
        public static event Action<Type> Changed;

        public static void Register<T>(T view) where T : class
        {
            if (view == null) return;
            Views[typeof(T)] = view;
            Changed?.Invoke(typeof(T));
        }

        public static void Unregister<T>(T view) where T : class
        {
            if (Views.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, view))
            {
                Views.Remove(typeof(T));
                Changed?.Invoke(typeof(T));
            }
        }

        /// <summary>Kayıtlı view ya da null. Her karede değil, gerektiğinde çağırın.</summary>
        public static T Get<T>() where T : class
        {
            return Views.TryGetValue(typeof(T), out var view) ? view as T : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Views.Clear();
            Changed = null;
        }
    }

    /// <summary>
    /// UI view'ları için taban sınıf: etkinleşince kendini ViewLocator'a TInterface olarak kaydeder.
    /// Kullanım: <c>public class MyScaleView : RegisteredView&lt;IScaleView&gt;, IScaleView { ... }</c>
    /// </summary>
    public abstract class RegisteredView<TInterface> : MonoBehaviour where TInterface : class
    {
        protected virtual void OnEnable() => ViewLocator.Register(this as TInterface);
        protected virtual void OnDisable() => ViewLocator.Unregister(this as TInterface);
    }
}
