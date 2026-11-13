using System.Collections.Generic;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ağaç yerleşimi (saf C#): sütun = derinlik. İlk çocuk ebeveynin satırında devam eder (ana dal düz gider);
    /// diğer çocuklar, alt ağacının kapladığı sütunlarda boş olan ilk satıra iner. Böylece çizgiler kesişmez.
    /// </summary>
    public static class AchievementLayout
    {
        /// <param name="parents">Her düğümün ebeveyn indeksi (-1 = kök). Ebeveyn çocuktan önce gelmeli.</param>
        /// <param name="columns">Çıktı: sütun.</param>
        /// <param name="rows">Çıktı: satır.</param>
        public static void Compute(IReadOnlyList<int> parents, int[] columns, int[] rows)
        {
            int n = parents.Count;
            var children = new List<int>[n];
            for (int i = 0; i < n; i++) children[i] = new List<int>();
            var roots = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int p = parents[i];
                if (p >= 0 && p < i) children[p].Add(i);
                else roots.Add(i);
            }

            // Derinlik ve alt ağacın en derin sütunu
            var maxColumn = new int[n];
            for (int i = 0; i < n; i++)
                columns[i] = parents[i] >= 0 && parents[i] < i ? columns[parents[i]] + 1 : 0;
            for (int i = n - 1; i >= 0; i--)
            {
                maxColumn[i] = columns[i];
                for (int c = 0; c < children[i].Count; c++)
                    if (maxColumn[children[i][c]] > maxColumn[i]) maxColumn[i] = maxColumn[children[i][c]];
            }

            int columnCount = 0;
            for (int i = 0; i < n; i++) if (maxColumn[i] + 1 > columnCount) columnCount = maxColumn[i] + 1;
            var nextFree = new int[columnCount];

            for (int r = 0; r < roots.Count; r++)
                Place(roots[r], FreeRow(roots[r], columns, maxColumn, nextFree), children, columns, maxColumn, rows, nextFree);
        }

        private static int FreeRow(int node, int[] columns, int[] maxColumn, int[] nextFree)
        {
            int row = 0;
            for (int c = columns[node]; c <= maxColumn[node]; c++)
                if (nextFree[c] > row) row = nextFree[c];
            return row;
        }

        private static void Place(int node, int row, List<int>[] children, int[] columns, int[] maxColumn, int[] rows, int[] nextFree)
        {
            rows[node] = row;
            if (nextFree[columns[node]] < row + 1) nextFree[columns[node]] = row + 1;
            var list = children[node];
            for (int i = 0; i < list.Count; i++)
            {
                int child = list[i];
                int childRow = i == 0 ? row : FreeRow(child, columns, maxColumn, nextFree);
                Place(child, childRow, children, columns, maxColumn, rows, nextFree);
            }
        }
    }
}
