using System;
using System.Collections.Generic;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Lonca Sınavı havuzu (03 §5): havuzdan rastgele N soru, aynı oturumda tekrar yok; yanlış soru havuzdan
    /// başka bir soruyla değişir (ezberi engeller). Havuz biterse soru aynen tekrar sorulur. Saf C#, testli.
    /// </summary>
    public sealed class GuildExamPool<T> where T : class
    {
        private readonly List<T> remaining = new List<T>();
        private readonly Random rng;

        public GuildExamPool(IEnumerable<T> pool, Random rng)
        {
            this.rng = rng ?? new Random();
            foreach (var item in pool)
                if (item != null && !remaining.Contains(item)) remaining.Add(item);
        }

        public int Remaining => remaining.Count;

        /// <summary>count soru çeker (havuzdan çıkar). count &lt;= 0 ya da havuzdan büyükse hepsi.</summary>
        public List<T> Draw(int count)
        {
            if (count <= 0 || count > remaining.Count) count = remaining.Count;
            var result = new List<T>(count);
            for (int i = 0; i < count; i++) result.Add(TakeRandom());
            return result;
        }

        /// <summary>Yanlış sorunun yerine havuzdan başka bir soru; havuz boşsa false (aynı soru kalır).</summary>
        public bool TryReplace(out T replacement)
        {
            if (remaining.Count == 0)
            {
                replacement = null;
                return false;
            }
            replacement = TakeRandom();
            return true;
        }

        private T TakeRandom()
        {
            int i = rng.Next(remaining.Count);
            var item = remaining[i];
            remaining.RemoveAt(i);
            return item;
        }
    }
}
