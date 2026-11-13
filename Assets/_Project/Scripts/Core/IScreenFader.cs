using System.Collections;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Sahne geçişi kararması. UI tarafı (Mustafa) bu arayüzü uygulayan bir view yazar ve
    /// OnEnable'da SceneFlow.RegisterFader(this) çağırır. Fader yoksa geçiş kararmadan olur.
    /// </summary>
    public interface IScreenFader
    {
        IEnumerator FadeOut(float duration);
        IEnumerator FadeIn(float duration);
    }
}
