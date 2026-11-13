using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Sürücünün FPS kolları (görünüm). Bicycle binince ellerin tutacağı iki noktayı verir; kollar oraya uzanır,
    /// sürerken gidonla (dönüş, yatış, sallanma) birlikte hareket eder, inince bırakır. Mantık bilmez.
    /// </summary>
    public interface IRiderArmsView
    {
        /// <param name="leftGrip">Sol elin tutacağı nokta. Dönüşü = sol elin hedef dönüşü.</param>
        /// <param name="rightGrip">Sağ elin tutacağı nokta. Dönüşü = sağ elin hedef dönüşü.</param>
        /// <param name="body">Gövde yönü buradan alınır (yalnızca yatay dönüş); kollar kameraya değil gövdeye bağlanır.</param>
        /// <param name="instant">Uzanma geçişi olmadan doğrudan tut (kayıttan devam).</param>
        void BeginGrip(Transform leftGrip, Transform rightGrip, Transform body, bool instant);

        /// <summary>Eller gidonu bırakır ve kollar normal pozuna yumuşakça döner.</summary>
        void EndGrip();
    }
}
