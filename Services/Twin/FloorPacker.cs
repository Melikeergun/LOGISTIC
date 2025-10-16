using System.Collections.Generic;
using System.Linq;
using MLYSO.Web.Models.Twin;
using TwinContainerType = MLYSO.Web.Models.Twin.ContainerType;

namespace MLYSO.Web.Services.Twin
{
    public sealed class FloorPacker
    {
        private readonly int _floorW, _floorL;

        public FloorPacker(int floorWidthMm, int floorLengthMm)
        {
            _floorW = floorWidthMm;
            _floorL = floorLengthMm;
        }

        public sealed record Rect(int W, int L, TwinContainerType C);
        public sealed record PlacedRect(int X, int Y, int W, int L, int RotDeg, TwinContainerType C);

        public List<PlacedRect> Pack(IEnumerable<TwinContainerType> containers)
        {
            var skyline = new List<(int X, int Y, int Width)> { (0, 0, _floorW) };
            var placed = new List<PlacedRect>();

            var list = containers.OrderByDescending(c => c.InnerL * c.InnerW).ToList();

            foreach (var c in list)
            {
                var cand = new[]
                {
                    new { W = c.InnerW, L = c.InnerL, Rot = 0  },
                    new { W = c.InnerL, L = c.InnerW, Rot = 90 }
                };

                (int x, int y, int rot, int w, int l)? best = null;
                int bestY = int.MaxValue;

                foreach (var o in cand)
                {
                    var pos = FindPosition(new Rect(o.W, o.L, c), skyline);
                    if (pos is null) continue;
                    if (pos.Value.y < bestY)
                    {
                        best = (pos.Value.x, pos.Value.y, o.Rot, o.W, o.L);
                        bestY = pos.Value.y;
                    }
                }

                if (best is not null)
                    Place(new Rect(best.Value.w, best.Value.l, c), (best.Value.x, best.Value.y), skyline, placed);
            }

            for (int i = 0; i < placed.Count; i++)
            {
                var p = placed[i];
                placed[i] = p with { RotDeg = p.W > p.L ? 90 : 0 };
            }
            return placed;
        }

        private (int x, int y)? FindPosition(Rect r, List<(int X, int Y, int Width)> skyline)
        {
            (int x, int y)? best = null;
            int bestY = int.MaxValue;
            for (int i = 0; i < skyline.Count; i++)
            {
                var (sx, sy, sw) = skyline[i];
                if (r.W <= sw)
                {
                    if (sy < bestY && (sx + r.W) <= _floorW) { best = (sx, sy); bestY = sy; }
                }
            }
            return best;
        }

        private void Place(Rect r, (int x, int y) pos, List<(int X, int Y, int Width)> skyline, List<PlacedRect> placed)
        {
            placed.Add(new PlacedRect(pos.x, pos.y, r.W, r.L, r.W > r.L ? 90 : 0, r.C));

            for (int i = 0; i < skyline.Count; i++)
            {
                var (sx, sy, sw) = skyline[i];
                if (pos.x >= sx && pos.x < sx + sw)
                {
                    int leftWidth = pos.x - sx;
                    int rightWidth = (sx + sw) - (pos.x + r.W);
                    var newY = sy + r.L;

                    var newList = new List<(int, int, int)>();
                    if (leftWidth > 0) newList.Add((sx, sy, leftWidth));
                    newList.Add((pos.x, newY, r.W));
                    if (rightWidth > 0) newList.Add((pos.x + r.W, sy, rightWidth));

                    skyline.RemoveAt(i);
                    skyline.InsertRange(i, newList);
                    MergeSkyline(skyline);
                    return;
                }
            }
        }

        private void MergeSkyline(List<(int X, int Y, int Width)> skyline)
        {
            for (int i = 0; i < skyline.Count - 1;)
            {
                var a = skyline[i];
                var b = skyline[i + 1];
                if (a.Y == b.Y && a.X + a.Width == b.X)
                {
                    skyline[i] = (a.X, a.Y, a.Width + b.Width);
                    skyline.RemoveAt(i + 1);
                }
                else i++;
            }
        }
    }
}
