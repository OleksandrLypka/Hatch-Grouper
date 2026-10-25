namespace HatchGrouper
{
    /// <summary>
    /// Finds the minimal number of groups such that, inside every group,
    /// both width and height differ by at most Settings.ToleranceMm.
    /// Geometrically: cover all (width, height) points with the fewest tol x tol squares.
    /// </summary>
    public static class Grouper
    {
        private const long NodeLimit = 2_000_000; // safety stop for very large inputs

        public static List<NicheGroup> Build(IList<Niche> niches, Settings s)
        {
            if (niches.Count == 0) throw new UserException(Loc.T("err.empty"));
            if (s.ToleranceMm < 0)
                throw new UserException(Loc.T("err.tolerance", s.TrimMm, s.MinLapMm, s.MaxGapMm, s.GapMm));
            int tol = s.ToleranceMm;

            // 1. Distinct sizes are the points to cover.
            var sizes = niches.Select(n => (W: n.WidthMm, H: n.HeightMm)).Distinct().ToList();

            // 2. Candidate windows: lower-left corner at every existing width/height value.
            var windows = new List<int[]>();
            foreach (var w0 in sizes.Select(p => p.W).Distinct())
            {
                foreach (var h0 in sizes.Select(p => p.H).Distinct())
                {
                    var inside = Enumerable.Range(0, sizes.Count)
                        .Where(i => sizes[i].W >= w0 && sizes[i].W <= w0 + tol &&
                                    sizes[i].H >= h0 && sizes[i].H <= h0 + tol)
                        .ToArray();
                    if (inside.Length > 0) windows.Add(inside);
                }
            }

            // 3. Keep only maximal windows (drop subsets and duplicates).
            var all = windows;
            windows = all
                .Where(a => !all.Any(b => b.Length > a.Length && a.All(i => b.Contains(i))))
                .GroupBy(a => string.Join(",", a))
                .Select(g => g.First())
                .ToList();

            // 4. Exact set cover by depth-first search with pruning.
            var search = new Search { Windows = windows, PointCount = sizes.Count };
            search.Run(new bool[sizes.Count], 0, new List<int[]>());

            // 5. Give every distinct size to exactly one chosen window (largest windows first).
            var owner = new int[sizes.Count];
            Array.Fill(owner, -1);
            int groupCount = 0;
            foreach (var w in search.Best.OrderByDescending(w => w.Length))
            {
                var members = w.Where(i => owner[i] < 0).ToList();
                if (members.Count == 0) continue;
                foreach (var i in members) owner[i] = groupCount;
                groupCount++;
            }

            // 6. Build the groups and compute box/hatch sizes.
            var groups = new List<NicheGroup>();
            for (int g = 0; g < groupCount; g++) groups.Add(new NicheGroup());
            foreach (var n in niches)
            {
                int idx = sizes.IndexOf((n.WidthMm, n.HeightMm));
                groups[owner[idx]].Niches.Add(n);
            }
            foreach (var g in groups) g.Compute(s);

            return groups.OrderBy(g => g.MinH).ThenBy(g => g.MinW).ToList();
        }

        private sealed class Search
        {
            public List<int[]> Windows;
            public int PointCount;
            public List<int[]> Best;
            private long _nodes;

            public void Run(bool[] covered, int coveredCount, List<int[]> chosen)
            {
                if (Best != null && chosen.Count >= Best.Count) return; // cannot beat the best
                if (Best != null && ++_nodes > NodeLimit) return;       // keep the best found so far
                if (coveredCount == PointCount) { Best = new List<int[]>(chosen); return; }

                // Pick the uncovered point that is contained in the fewest windows.
                int target = -1, fewest = int.MaxValue;
                for (int p = 0; p < PointCount; p++)
                {
                    if (covered[p]) continue;
                    int c = Windows.Count(w => w.Contains(p));
                    if (c < fewest) { fewest = c; target = p; }
                }

                // Try windows containing it, those covering most new points first.
                var options = Windows.Where(w => w.Contains(target))
                                     .OrderByDescending(w => w.Count(i => !covered[i]))
                                     .ToList();
                foreach (var w in options)
                {
                    var fresh = w.Where(i => !covered[i]).ToArray();
                    foreach (var i in fresh) covered[i] = true;
                    chosen.Add(w);
                    Run(covered, coveredCount + fresh.Length, chosen);
                    chosen.RemoveAt(chosen.Count - 1);
                    foreach (var i in fresh) covered[i] = false;
                }
            }
        }
    }
}
