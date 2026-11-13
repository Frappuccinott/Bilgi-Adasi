using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Figma tasarımlı view'ların tabanı: temayı verir ve sahnede aynı arayüzü uygulayan yer tutucu varsa
    /// (PlaceholderWorkshopHud gibi) Start'ta yeniden kaydolarak onun önüne geçer.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public abstract class FigmaView<TInterface> : RegisteredView<TInterface> where TInterface : class
    {
        [Tooltip("Boş = Resources/UiTheme ya da varsayılan palet.")]
        [SerializeField] private UiTheme theme;

        protected UiTheme Theme => UiTheme.OrDefault(theme);

        protected virtual void Start() => ViewLocator.Register(this as TInterface);
    }
}
