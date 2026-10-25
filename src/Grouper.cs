namespace HatchGrouper
{
    /// <summary>
    /// Finds the minimal number of groups such that, inside every group,
    /// width differs by at most Settings.ToleranceWMm and height by at most Settings.ToleranceHMm.
    /// Geometrically: cover all (width, height) points with the fewest tolW x tolH rectangles,
    /// then assign points that fit into several chosen rectangles so that the maximum gap is minimal.
    /// </summary>
    public static class Grouper
    {
        private const long NodeLimit = 2_000_000;       // safety stop for very large inputs (set cover)
        private const long AssignNodeLimit = 200_000;   // safety stop for borderline-point assignment

        public static List<NicheGroup> Build(IList<Niche> niches, Settings s)
        {
            if (niches.Count == 0) throw new UserException(Loc.T("err.empty"));
            if (s.ToleranceWMm < 0)
                throw new UserException(Loc.T("err.tolerance", s.TrimMm, s.MinLapMm, s.MaxGapWMm, s.GapMm));
            if (s.ToleranceHMm < 0)
                throw new UserException(Loc.T("err.tolerance", s.EffTrimHMm, s.EffMinLapHMm, s.MaxGapHMm, s.EffGapHMm));

            int tolW = s.ToleranceWMm;
            int tolH = s.ToleranceHMm;

            // 1. Distinct sizes are the points to cover.
            var sizes = niches.Select(n => (W: n.WidthMm, H: n.HeightMm)).Distinct().ToList();

            // 2. Candidate windows: lower-left corner at every existing width/height value.
            var windows = new List<int[]>();
            foreach (var w0 in sizes.Select(p => p.W).Distinct())
            {
                foreach (var h0 in sizes.Select(p => p.H).Distinct())
                {
                    var inside = Enumerable.Range(0, sizes.Count)
                        .Where(i => sizes[i].W >= w0 && sizes[i].W <= w0 + tolW &&
                                    sizes[i].H >= h0 && sizes[i].H <= h0 + tolH)
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

            // 5. Assign every distinct size to one of the chosen windows that contain it,
            //    minimising the worst gap across groups (and then the total gap / spread).
            var chosenWindows = search.Best;
            var owner = AssignPointsOptimally(sizes, chosenWindows, s);

            // 6. Build the groups (skipping any empty window) and compute box/hatch sizes.
            var groupMap = new Dictionary<int, NicheGroup>();
            foreach (var n in niches)
            {
                int idx = sizes.IndexOf((n.WidthMm, n.HeightMm));
                int wIdx = owner[idx];
                if (!groupMap.TryGetValue(wIdx, out var group))
                {
                    group = new NicheGroup();
                    groupMap[wIdx] = group;
                }
                group.Niches.Add(n);
            }
            var groups = groupMap.Values.ToList();
            foreach (var g in groups) g.Compute(s);

            return groups.OrderBy(g => g.MinH).ThenBy(g => g.MinW).ToList();
        }

        /// <summary>
        /// Assigns each distinct size to one containing window from <paramref name="chosenWindows"/>
        /// so that the maximum gap across all groups (and then the sum of gaps and spreads) is minimal.
        /// </summary>
        private static int[] AssignPointsOptimally(List<(int W, int H)> sizes, List<int[]> chosenWindows, Settings s)
        {
            int pointCount = sizes.Count;
            int winCount = chosenWindows.Count;

            // For each point, list the indices of chosen windows that can hold it
            var allowed = new int[pointCount][];
            for (int p = 0; p < pointCount; p++)
            {
                allowed[p] = Enumerable.Range(0, winCount)
                    .Where(w => chosenWindows[w].Contains(p))
                    .ToArray();
            }

            var assigner = new Assigner(sizes, allowed, winCount, s.GapMm, s.EffGapHMm);
            return assigner.Solve();
        }

        private sealed class Assigner
        {
            private readonly List<(int W, int H)> _sizes;
            private readonly int[][] _allowed;
            private readonly int _winCount;
            private readonly int _twoGapW;
            private readonly int _twoGapH;

            private readonly int[] _minW, _maxW, _minH, _maxH, _count;
            private readonly int[] _currentOwner;
            private readonly int[] _bestOwner;
            private readonly List<int> _flexible;

            private int _bestWorstTwoGap = int.MaxValue;
            private int _bestSumTwoGap = int.MaxValue;
            private int _bestSumSpread = int.MaxValue;
            private long _nodes;

            public Assigner(List<(int W, int H)> sizes, int[][] allowed, int winCount, int gapWMm, int gapHMm)
            {
                _sizes = sizes;
                _allowed = allowed;
                _winCount = winCount;
                _twoGapW = 2 * gapWMm;
                _twoGapH = 2 * gapHMm;

                _minW = new int[winCount];
                _maxW = new int[winCount];
                _minH = new int[winCount];
                _maxH = new int[winCount];
                _count = new int[winCount];
                Array.Fill(_minW, int.MaxValue);
                Array.Fill(_minH, int.MaxValue);

                _currentOwner = new int[sizes.Count];
                _bestOwner = new int[sizes.Count];
                _flexible = new List<int>();
            }

            public int[] Solve()
            {
                // 1. Place points that only fit into a single chosen window
                for (int p = 0; p < _sizes.Count; p++)
                {
                    if (_allowed[p].Length == 1)
                    {
                        int g = _allowed[p][0];
                        _currentOwner[p] = g;
                        AddToGroup(g, _sizes[p].W, _sizes[p].H);
                    }
                    else
                    {
                        _flexible.Add(p);
                    }
                }

                // Order flexible points: fewest candidate groups first
                _flexible.Sort((a, b) => _allowed[a].Length.CompareTo(_allowed[b].Length));

                // 2. Branch-and-bound over flexible (overlapping) points
                SearchFlexible(0);
                return _bestOwner;
            }

            private void AddToGroup(int g, int w, int h)
            {
                if (_count[g] == 0)
                {
                    _minW[g] = _maxW[g] = w;
                    _minH[g] = _maxH[g] = h;
                }
                else
                {
                    if (w < _minW[g]) _minW[g] = w;
                    if (w > _maxW[g]) _maxW[g] = w;
                    if (h < _minH[g]) _minH[g] = h;
                    if (h > _maxH[g]) _maxH[g] = h;
                }
                _count[g]++;
            }

            private void SearchFlexible(int idx)
            {
                if (_bestWorstTwoGap < int.MaxValue && ++_nodes > AssignNodeLimit) return;

                // Evaluate current bounding boxes (monotonic lower bound for partial assignments)
                int worstTwoGap = 0, sumTwoGap = 0, sumSpread = 0;
                for (int g = 0; g < _winCount; g++)
                {
                    if (_count[g] == 0) continue;
                    int sw = _maxW[g] - _minW[g];
                    int sh = _maxH[g] - _minH[g];
                    int tg = Math.Max(_twoGapW + sw, _twoGapH + sh);
                    if (tg > worstTwoGap) worstTwoGap = tg;
                    sumTwoGap += tg;
                    sumSpread += sw + sh;
                }

                // Prune if already no better than the best complete assignment
                if (IsWorseOrEqual(worstTwoGap, sumTwoGap, sumSpread)) return;

                if (idx == _flexible.Count)
                {
                    _bestWorstTwoGap = worstTwoGap;
                    _bestSumTwoGap = sumTwoGap;
                    _bestSumSpread = sumSpread;
                    Array.Copy(_currentOwner, _bestOwner, _currentOwner.Length);
                    return;
                }

                int p = _flexible[idx];
                int w = _sizes[p].W, h = _sizes[p].H;
                var candidates = _allowed[p];

                // Try the group where adding (w, h) produces the smallest gap / spread increase first
                if (candidates.Length > 1)
                {
                    candidates = candidates
                        .OrderBy(g => CostAfterAdding(g, w, h))
                        .ToArray();
                }

                foreach (int g in candidates)
                {
                    int oldMinW = _minW[g], oldMaxW = _maxW[g], oldMinH = _minH[g], oldMaxH = _maxH[g], oldCount = _count[g];
                    _currentOwner[p] = g;
                    AddToGroup(g, w, h);

                    SearchFlexible(idx + 1);

                    _minW[g] = oldMinW; _maxW[g] = oldMaxW; _minH[g] = oldMinH; _maxH[g] = oldMaxH; _count[g] = oldCount;
                }
            }

            private long CostAfterAdding(int g, int w, int h)
            {
                if (_count[g] == 0) return 0;
                int oldSw = _maxW[g] - _minW[g];
                int oldSh = _maxH[g] - _minH[g];
                int newSw = Math.Max(_maxW[g], w) - Math.Min(_minW[g], w);
                int newSh = Math.Max(_maxH[g], h) - Math.Min(_minH[g], h);
                int newTwoGap = Math.Max(_twoGapW + newSw, _twoGapH + newSh);
                int deltaSpread = (newSw - oldSw) + (newSh - oldSh);
                return ((long)newTwoGap << 20) + deltaSpread;
            }

            private bool IsWorseOrEqual(int worstTwoGap, int sumTwoGap, int sumSpread)
            {
                if (worstTwoGap != _bestWorstTwoGap) return worstTwoGap > _bestWorstTwoGap;
                if (sumTwoGap != _bestSumTwoGap) return sumTwoGap > _bestSumTwoGap;
                return sumSpread >= _bestSumSpread;
            }
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
