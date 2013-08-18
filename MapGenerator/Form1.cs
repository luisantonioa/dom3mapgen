using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BenTools.Mathematics;
using ImageTools.Core;
using LambdaImageProcessing.Core;
using MapGenerator.Properties;

namespace MapGenerator
{
    public partial class Form1 : Form
    {
        private List<Vector> _pointList;
        public List<Vector> OrderedPoints;
        private VoronoiGraph _graph;
        private MultiValueDictionary<Vector, Vector> _provinces;

        public Random Random;

        private readonly ImageAttributes _imageAttributes;

        private Matrix _upMatrix, _downMatrix, _leftMatric, _rightMatrix;

        public Form1()
        {
            InitializeComponent();

            txtSeed.Maximum = Int32.MaxValue;
            _imageAttributes = new ImageAttributes();
            _imageAttributes.SetWrapMode(WrapMode.Tile);
            GeneratePens();
            AddTextureBrush("maptile", "images/maptile.png");
            AddTextureBrush("mountain1", "images/mountain1.png");
            AddTextureBrush("mountain2", "images/mountain2.png");
            AddTextureBrush("mountain3", "images/mountain3.png");
            AddTextureBrush("mountain4", "images/mountain4.png");
            const double treeScale = 0.25;
            AddTextureBrush("tree1", "images/tree1.png", treeScale);
            AddTextureBrush("tree2", "images/tree2.png", treeScale);
            AddTextureBrush("tree3", "images/tree3.png", treeScale);
            AddTextureBrush("tree4", "images/tree4.png", treeScale);
        }

        private void GenerateMapFromSeed()
        {
            btnGenerateMap.Enabled = false;
            Random = new Random(Convert.ToInt32(txtSeed.Value));
            GeneratePoints();
            ComputeVoronoi();
            GenerateProvinces();

            for (int i = 0; i < Convert.ToInt32(txtPointImprovements.Value); i++)
            {
                ImprovePoints();
                ComputeVoronoi();
                GenerateProvinces();
            }

            using (var g = Graphics.FromImage(picDiagram.Image))
            {
                g.Clear(Color.Wheat);
                var unit = GraphicsUnit.Pixel;
                g.FillRectangle(_textures["maptile"], picDiagram.Image.GetBounds(ref unit));
            }
            picDiagram.Invalidate();

            DrawJaggedEdges();

            GenerateTerrain();
            btnGenerateMap.Enabled = true;
        }

        private void DrawPoints()
        {
            var height = Convert.ToInt32(txtHeight.Value);
            var width = Convert.ToInt32(txtWidth.Value);

            var g = Graphics.FromImage(picDiagram.Image);
            g.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);

            foreach (var vector in _pointList)
            {
                g.FillRectangle(Brushes.Black, (float)vector[0] - 5, (float)vector[1] - 5, 10, 10);
            }

            picDiagram.Refresh();
        }

        private void GeneratePoints(int seed = -1)
        {
            var points = Convert.ToInt32(txtPoints.Value);

            picDiagram.Image = new Bitmap(Width(), Height(), PixelFormat.Format32bppPArgb);

            _pointList = new List<Vector>(points);

            for (int i = 0; _pointList.Count < points; i++)
            {
                var x = Random.NextDouble() * (Width() - 20) + 10;
                var y = Random.NextDouble() * (Height() - 20) + 10;
                var vector = new Vector(x, y);

                if (_pointList.Count > 0 && _pointList.Select(v => Distance(v, vector)).OrderBy(v => v).First() < 30)
                    continue;

                _pointList.Add(vector);
            }

            OrderedPoints = _pointList
                .OrderBy(v => String.Format("{0:000000000}{1:000000000}", Height() - (int)v[1], (int)v[0]))
                .ToList();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    for (int i = 0; i < points; i++)
                    {
                        var point = _pointList[i];
                        _pointList.Add(new Vector(point[0] + (x * Width()), point[1] + (y * Height())));
                    }
                }
            }
        }

        private void ComputeVoronoi()
        {
            _graph = Fortune.ComputeVoronoiGraph(_pointList);
        }

        private void DrawVoronoi()
        {
            DrawPoints();
            var g = Graphics.FromImage(picDiagram.Image);
            foreach (VoronoiEdge edge in _graph.Edges)
            {
                g.DrawLine(Pens.Brown, (float)edge.VVertexA[0], (float)edge.VVertexA[1], (float)edge.VVertexB[0],
                           (float)edge.VVertexB[1]);
            }

            foreach (Vector vector in _graph.Vertizes)
            {
                g.FillEllipse(Brushes.Red, (float)vector[0] - 5, (float)vector[1] - 5, 10, 10);
            }

            g.Dispose();
            picDiagram.Refresh();
        }

        public void DrawNeighbours()
        {
            var g = Graphics.FromImage(picDiagram.Image);

            var keys = Map.Neighbours.Keys.ToList();
            keys.Sort();
            foreach (var provId in keys)
            {
                var provPoint = OrderedPoints[provId - 1];
                foreach (var neighbourId in Map.Neighbours[provId])
                {
                    var neighbourPoint = OrderedPoints[neighbourId - 1];
                    g.DrawLine(Pens.Blue, (float)provPoint[0], (float)provPoint[1], (float)neighbourPoint[0], (float)neighbourPoint[1]);
                }
            }

            g.Dispose();
            picDiagram.Refresh();
        }

        private Vector WrapVector(Vector vector)
        {
            var newVector = new Vector(2);
            newVector[0] = (vector[0] + Map.Width) % Map.Width;
            newVector[1] = (vector[1] + Map.Height) % Map.Height;
            return newVector;
        }

        private Point WrapPoint(Point point)
        {
            return new Point((point.X + Map.Width) % Map.Width, (point.Y + Map.Height) % Map.Height);
        }

        public void OrderPoints(VoronoiEdge edge, out PointF pointA, out PointF pointB)
        {
            var x1 = (int)edge.VVertexA[0];
            var y1 = (int)edge.VVertexA[1];
            var x2 = (int)edge.VVertexB[0];
            var y2 = (int)edge.VVertexB[1];

            if (x1 > x2 || (x1 == x2 && y1 > y2))
            {
                pointA = edge.VVertexB.ToPointF();
                pointB = edge.VVertexA.ToPointF();
            }
            else
            {
                pointA = edge.VVertexA.ToPointF();
                pointB = edge.VVertexB.ToPointF();
            }
        }

        private readonly Dictionary<Rectangle, Point[]> _indexToPath = new Dictionary<Rectangle, Point[]>();

        public Dominons3Map Map;
        private void GenerateProvinces()
        {
            _rightMatrix = new Matrix();
            _rightMatrix.Translate(Width(), 0);
            _leftMatric = new Matrix();
            _leftMatric.Translate(-1 * Width(), 0);
            _downMatrix = new Matrix();
            _downMatrix.Translate(0, Height());
            _upMatrix = new Matrix();
            _upMatrix.Translate(0, -1 * Height());

            Map = new Dominons3Map();
            Map.MapImage = picDiagram.Image;
            Map.Title = "Test Map";
            Map.Filename = "TestMap";
            Map.IsWrapAround = true;
            Map.Description = "Description of Dom 3 Map.";
            Map.ProvinceCount = OrderedPoints.Count;

            var neighbours = new MultiValueDictionary<Vector, Vector>();

            _provinces = new MultiValueDictionary<Vector, Vector>();
            var height = Convert.ToInt32(txtHeight.Value);
            var width = Convert.ToInt32(txtWidth.Value);

            _indexToPath.Clear();

            MapBounds = new Rectangle(0, 0, Width(), Height());
            _borderPointX = new PointF(Width(), 0);
            _borderPointEdge = new PointF(Width(), Height());
            _borderPointY = new PointF(0, Height());

            var visibleEdges = _graph.Edges
                .Cast<VoronoiEdge>()
                .Where(edge => IsVertexValid(edge.VVertexA) && IsVertexValid(edge.VVertexB) && (MapBounds.ContainsEdge(edge) || MapBounds.IntersectsEdge(edge)))
                .ToArray();

            foreach (var edge in visibleEdges)
            {
                var segmentId = GetSegmentId(edge);

                if (!_indexToPath.ContainsKey(segmentId))
                    _indexToPath[segmentId] = GenerateJaggedSegment(edge);
            }

            foreach (VoronoiEdge edge1 in _graph.Edges)
            {
                if (!IsOutofBounds(height, width, edge1.LeftData) && !IsOutofBounds(height, width, edge1.RightData))
                {
                    neighbours.Add(edge1.LeftData, edge1.RightData);
                    neighbours.Add(edge1.RightData, edge1.LeftData);

                    var leftProvinceId = OrderedPoints.IndexOf(edge1.LeftData) + 1;
                    var rightProvinceId = OrderedPoints.IndexOf(edge1.RightData) + 1;

                    Map.Neighbours.Add(leftProvinceId, rightProvinceId);
                    Map.Neighbours.Add(rightProvinceId, leftProvinceId);
                }

                if (!IsOutofBounds(height, width, edge1.LeftData) && IsOutofBounds(height, width, edge1.RightData))
                {

                    var leftProvinceId = OrderedPoints.IndexOf(edge1.LeftData) + 1;
                    var rightProvinceId = OrderedPoints.IndexOf(WrapVector(edge1.RightData)) + 1;

                    Map.Neighbours.Add(leftProvinceId, rightProvinceId);
                    Map.Neighbours.Add(rightProvinceId, leftProvinceId);
                }

                if (IsOutofBounds(height, width, edge1.LeftData) && !IsOutofBounds(height, width, edge1.RightData))
                {

                    var leftProvinceId = OrderedPoints.IndexOf(WrapVector(edge1.LeftData)) + 1;
                    var rightProvinceId = OrderedPoints.IndexOf(edge1.RightData) + 1;

                    Map.Neighbours.Add(leftProvinceId, rightProvinceId);
                    Map.Neighbours.Add(rightProvinceId, leftProvinceId);
                }

                if (!IsOutofBounds(height, width, edge1.LeftData))
                {
                    _provinces.Add(edge1.LeftData, edge1.VVertexA);
                    _provinces.Add(edge1.LeftData, edge1.VVertexB);
                }
                if (!IsOutofBounds(height, width, edge1.RightData))
                {
                    _provinces.Add(edge1.RightData, edge1.VVertexA);
                    _provinces.Add(edge1.RightData, edge1.VVertexB);
                }
            }

            for (int i = 0; i < OrderedPoints.Count; i++)
            {
                var provinceId = i + 1;
                Map.TerrainTypes[provinceId] = 0;

            }

            using (var g = Graphics.FromImage(picDiagram.Image))
            {
                foreach (var key in _provinces.Keys.ToList())
                {
                    var list = _provinces[key];
                    var f = list.First();

                    Vector key1 = key;
                    var points = list
                        .Select(v => new { angle = GetAngle(f, key1, v), v })
                        .OrderBy(a => a.angle)
                        .Select(a => a.v)
                        .ToList();

                    _provinces[key] = new HashSet<Vector>(points);

                    var randomBrush = GetRandomBrush();
                    g.FillPolygon(randomBrush, points.Select(v => v.ToPointF()).ToArray());
                    g.FillPolygon(randomBrush, points.Select(v => new PointF((float)v[0] + width, (float)v[1])).ToArray());
                    g.FillPolygon(randomBrush, points.Select(v => new PointF((float)v[0] - width, (float)v[1])).ToArray());
                    g.FillPolygon(randomBrush, points.Select(v => new PointF((float)v[0], (float)v[1] + height)).ToArray());
                    g.FillPolygon(randomBrush, points.Select(v => new PointF((float)v[0], (float)v[1] - height)).ToArray());
                    var provId = OrderedPoints.IndexOf(key);
                    g.DrawString(provId.ToString(), _provFont, Brushes.Black, (float)key[0] + 5f, (float)key[1] + 5f);
                }
            }
            picDiagram.Invalidate();
        }

        private bool IsVertexValid(Vector vector)
        {
            return !Double.IsNaN(vector[0]) && !Double.IsNaN(vector[1]);
        }

        private Rectangle GetSegmentId(PointF point1, PointF point2)
        {
            return GetSegmentId(point1.ToPoint(), point2.ToPoint());
        }

        private Rectangle GetSegmentId(Point point1, Point point2)
        {
            if (!MapBounds.ContainsLine(point1, point2) && !MapBounds.IntersectsLine(point1, point2))
            {
                var newPoint1 = WrapPoint(point1);
                var diff = new Size(point1.X - newPoint1.X, point1.Y - newPoint1.Y);
                point1 = newPoint1;
                point2 = Point.Subtract(point2, diff);
            }

            if (Utils.LineIntersectsLine(point1, point2, _borderPointX, _borderPointEdge))
            {
                point1.X -= Width();
                point2.X -= Width();
            }

            if (Utils.LineIntersectsLine(point1, point2, _borderPointY, _borderPointEdge))
            {
                point1.Y -= Height();
                point2.Y -= Height();
            }

            return new Rectangle(Math.Min(point1.X, point2.X), Math.Min(point1.Y, point2.Y), Math.Abs(point1.X - point2.X), Math.Abs(point1.Y - point2.Y));
        }

        private Rectangle GetSegmentId(VoronoiEdge edge)
        {
            return GetSegmentId(edge.VVertexA.ToPointF(), edge.VVertexB.ToPointF());
        }

        private Point[] GenerateJaggedSegment(VoronoiEdge edge)
        {
            var t = Interpolate(edge.VVertexA, edge.LeftData, NoisyLineTradeoff);
            var q = Interpolate(edge.VVertexA, edge.RightData, NoisyLineTradeoff);
            var r = Interpolate(edge.VVertexB, edge.LeftData, NoisyLineTradeoff);
            var s = Interpolate(edge.VVertexB, edge.RightData, NoisyLineTradeoff);

            var midpoint = Interpolate(edge.VVertexA, edge.VVertexB, 0.5);

            var points = new List<Point>();
            var segment1 = BuildNoisyLineSegments(edge.VVertexA, t, midpoint, q, MinimumJaggedLength);
            points.AddRange(segment1.Select(v => v.ToPoint()));
            var segment2 = BuildNoisyLineSegments(edge.VVertexB, s, midpoint, r, MinimumJaggedLength);
            points.AddRange(segment2.Select(v => v.ToPoint()).Reverse());

            return points.ToArray();
        }

        private Brush GetRandomBrush()
        {
            int red = Random.Next(0, byte.MaxValue + 1);
            int green = Random.Next(0, byte.MaxValue + 1);
            int blue = Random.Next(0, byte.MaxValue + 1);
            return new SolidBrush(Color.FromArgb(red, green, blue));
        }

        private static double GetAngle(Vector a, Vector b, Vector c)
        {
            var v1 = new PointD(a[0] - b[0], a[1] - b[1]);
            var v2 = new PointD(c[0] - b[0], c[1] - b[1]);

            var angle = -(180 / Math.PI) * Math.Atan2(v1.X * v2.Y - v1.Y * v2.X, v1.X * v2.X + v1.Y * v2.Y);
            if (angle < 0)
                angle = 360 + angle;
            return angle;
        }

        private static bool IsOutofBounds(int height, int width, Vector pointD)
        {
            return pointD[0] < 0 || pointD[0] > width || pointD[1] < 0 || pointD[1] > height;
        }

        // Improve the random set of points with Lloyd Relaxation.
        private void ImprovePoints()
        {
            // We'd really like to generate "blue noise". Algorithms:
            // 1. Poisson dart throwing: check each new point against all
            //     existing points, and reject it if it's too close.
            // 2. Start with a hexagonal grid and randomly perturb points.
            // 3. Lloyd Relaxation: move each point to the centroid of the
            //     generated Voronoi polygon, then generate Voronoi again.
            // 4. Use force-based layout algorithms to push points away.
            // 5. More at http://www.cs.virginia.edu/~gfx/pubs/antimony/
            // Option 3 is implemented here. If it's run for too many iterations,
            // it will turn into a grid, but convergence is very slow, and we only
            // run it a few times.
            var newPoints = new List<Vector>();

            foreach (var key in _provinces.Keys)
            {
                var provincePoints = _provinces[key];
                var newpoint = new Vector(2);
                foreach (var point in provincePoints)
                {
                    newpoint.Add(point);
                }
                newpoint[0] /= provincePoints.Count;
                newpoint[1] /= provincePoints.Count;

                newpoint[0] = (newpoint[0] + key[0]) / 2;
                newpoint[1] = (newpoint[1] + key[1]) / 2;

                newPoints.Add(newpoint);
            }


            var height = Convert.ToInt32(txtHeight.Value);
            var width = Convert.ToInt32(txtWidth.Value);
            var points = Convert.ToInt32(txtPoints.Value);

            OrderedPoints = newPoints
                .Select(WrapVector)
                .OrderBy(v => String.Format("{0:000000000}{1:000000000}", height - (int)v[1], (int)v[0]))
                .ToList();

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    for (var i = 0; i < points; i++)
                    {
                        var point = newPoints[i];
                        newPoints.Add(new Vector(point[0] + (x * width), point[1] + (y * height)));
                    }
                }
            }

            _pointList = newPoints;
            //Text = _pointList.Count.ToString();
            _provinces = null;
            picDiagram.Image = new Bitmap(width, height);
            Map.Neighbours = new MultiValueDictionary<int, int>();
        }

        public const double NoisyLineTradeoff = 0.5;  // low: jagged vedge; high: jagged dedge
        private Pen _borderShore, _oceanBorder;

        private void GeneratePens()
        {
            _borderShore = new Pen(Color.FromArgb(57, 37, 42), 2.3f) {DashStyle = DashStyle.Dash};
            _oceanBorder = new Pen(Color.DodgerBlue, 2.3f) {DashStyle = DashStyle.DashDotDot};
        }

        private const double Variance = 5;
        private const double Density = 6;
        public const int MinimumJaggedLength = 14;

        private void DrawJaggedEdges()
        {
            using (var g = Graphics.FromImage(picDiagram.Image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                foreach (var pointList in _indexToPath.Values)
                {
                    g.Transform = new Matrix();
                    g.DrawCurve(_borderShore, pointList, 1.0f);
                    g.Transform = _upMatrix;
                    g.DrawCurve(_borderShore, pointList, 1.0f);
                    g.Transform = _downMatrix;
                    g.DrawCurve(_borderShore, pointList, 1.0f);
                    g.Transform = _leftMatric;
                    g.DrawCurve(_borderShore, pointList, 1.0f);
                    g.Transform = _rightMatrix;
                    g.DrawCurve(_borderShore, pointList, 1.0f);
                }
            }

            picDiagram.Invalidate();

            var bitmap = ((Bitmap)picDiagram.Image);
            foreach (var province in _provinces.Keys)
            {
                bitmap.SetPixel((int)province[0], (int)province[1], Color.White);
            }
            picDiagram.Refresh();

            toolStripStatusLabel1.Text = Resources.Status_Done;

            cleanImage = null;
            _jaggedImage = null;

            Map.MapImage = (Image)picDiagram.Image.Clone();
        }

        private readonly Font _provFont = new Font("Arial", 16f, FontStyle.Bold);

        private Tuple<Vector, Vector> Box(IEnumerable<Vector> points)
        {
            var min = new Vector(2);
            var max = new Vector(2);

            min[0] = Double.MaxValue;
            min[1] = Double.MaxValue;
            max[0] = Double.MinValue;
            max[1] = Double.MinValue;

            foreach (var point in points)
            {
                min[0] = Math.Min(min[0], point[0]);
                min[1] = Math.Min(min[1], point[1]);
                max[0] = Math.Max(max[0], point[0]);
                max[1] = Math.Max(max[1], point[1]);
            }
            return new Tuple<Vector, Vector>(min, max);
        }

        private List<Vector> _poly;

        private bool PointInPolygon(Vector point, List<Vector> polyg, float scale = 1f)
        {
            if (Math.Abs(scale - 1f) > float.Epsilon)
            {
                var center = GetCenter(polyg);
                var transform = new Matrix();
                transform.Translate(-1f * (float)center[0], -1f * (float)center[1], MatrixOrder.Append);
                transform.Scale(1.2f, 1.2f, MatrixOrder.Append);
                transform.Translate((float)center[0], (float)center[1], MatrixOrder.Append);
                var newPoints = polyg.Select(p => new PointF((float)p[0], (float)p[1])).ToArray();
                transform.TransformPoints(newPoints);
                polyg = newPoints.Select(p => new Vector(p.X, p.Y)).ToList();
            }
            var newPoly = Fix(polyg);
            int i, j = newPoly.Count - 1;
            bool oddNodes = false;

            for (i = 0; i < newPoly.Count; i++)
            {
                if ((newPoly[i][1] < point[1] && newPoly[j][1] >= point[1] || newPoly[j][1] < point[1] && newPoly[i][1] >= point[1])
                && (newPoly[i][0] <= point[0] || newPoly[j][0] <= point[0]))
                {
                    if (newPoly[i][0] + (point[1] - newPoly[i][1]) / (newPoly[j][1] - newPoly[i][1]) * (newPoly[j][0] - newPoly[i][0]) < point[0])
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }

            return oddNodes;
        }

        private Vector GetCenter(ICollection<Vector> vectors)
        {
            var c = new Vector(2);
            foreach (var point in vectors)
            {
                c.Add(point);
            }
            c[0] /= vectors.Count;
            c[1] /= vectors.Count;
            return c;
        }

        private List<Vector> Fix(List<Vector> vectors)
        {
            var s = vectors[0];
            var c = new Vector(2);
            foreach (var point in vectors)
            {
                c.Add(point);
            }
            c[0] /= vectors.Count;
            c[1] /= vectors.Count;
            var sortedPoints = vectors
                .Select(v => new { angle = GetAngle(s, c, v), v })
                .OrderBy(a => a.angle);
            return sortedPoints
                .Select(a => a.v)
                .ToList();
        }

        // Helper function: build a single noisy line in a quadrilateral A-B-C-D,
        // and store the output points in a Vector.
        public List<Vector> BuildNoisyLineSegments(Vector A, Vector B, Vector C, Vector D, double minLength, bool isMountain = false)
        {
            _poly = new[] { A, B, C, D }.ToList();
            var points = new List<Vector>();
            points.Add(A);
            Subdivide(A, B, C, D, minLength, points);
            points.Add(C);
            return points;
        }

        private TaskScheduler uithread;

        private static double p, q;

        public void Subdivide(Vector A, Vector B, Vector C, Vector D, double minLength, List<Vector> points)
        {
            var d1 = Distance(A, C);
            var d2 = Distance(B, D);

            if (d1 < minLength || d2 < minLength)
            {
                return;
            }

            // Subdivide the quadrilateral
            p = ((Random.NextDouble() * 0.6) + 0.2);  // vertical (along A-D and B-C)
            q = ((Random.NextDouble() * 0.6) + 0.2);  // horizontal (along A-B and D-C)

            // Midpoints
            var E = Interpolate(A, D, p);
            var F = Interpolate(B, C, p);
            var G = Interpolate(A, B, q);
            var I = Interpolate(D, C, q);

            // Central point
            var H = Interpolate(E, F, q);

            // Divide the quad into subquads, but meet at H
            var s = 1.0 - ((Random.NextDouble() * 0.8) - 0.4);
            var t = 1.0 - ((Random.NextDouble() * 0.8) - 0.4);

            Subdivide(A, Interpolate(G, B, s), H, Interpolate(E, D, t), minLength, points);
            points.Add(H);
            Subdivide(H, Interpolate(F, C, s), C, Interpolate(I, D, t), minLength, points);
        }

        public static double Distance(Vector a, Vector b)
        {
            var dx = Math.Abs(a[0] - b[0]);
            var dy = Math.Abs(a[1] - b[1]);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static int Distance(PointF a, PointF b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);
            return (int)Math.Sqrt(dx * dx + dy * dy);
        }

        public static PointF Interpolate(PointF a, PointF b, float position)
        {
            return new PointF { X = b.X + ((a.X - b.X) * position), Y = b.Y + ((a.Y - b.Y) * position) };
        }

        public static Vector Interpolate(Vector a, Vector b, double position)
        {
            var point = new Vector(2);
            point[0] = b[0] + ((a[0] - b[0]) * position);
            point[1] = b[1] + ((a[1] - b[1]) * position);
            return point;
        }

        private readonly Dictionary<string, Brush> _textures = new Dictionary<string, Brush>();
        private readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();
        
        private void AddTextureBrush(string name, string filename, double scale = 1.0)
        {
            if (_textures.ContainsKey(name)) return;
            var image = Image.FromFile(filename);
            image = ResizeImage(image, (int)(image.Width * scale), (int)(image.Height * scale));
            _textures.Add(name, new TextureBrush(image));
            _images.Add(name, image);
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            //a holder for the result
            var result = new Bitmap(width, height);

            //use a graphics object to draw the resized image into the bitmap
            using (Graphics graphics = Graphics.FromImage(result))
            {
                //set the resize quality modes to high quality
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                //draw the image into the target bitmap
                graphics.DrawImage(image, 0, 0, result.Width, result.Height);
            }

            //return the resulting bitmap
            return result;
        }

        private int Width()
        {
            return picDiagram.Image == null ? Convert.ToInt32(txtWidth.Value) : picDiagram.Image.Width;
        }

        private int Height()
        {
            return picDiagram.Image == null ? Convert.ToInt32(txtHeight.Value) : picDiagram.Image.Height;
        }

        private double[] heights;

        private Image _jaggedImage;
        private void GenerateTerrain()
        {
            if (_jaggedImage == null)
                _jaggedImage = (Image)picDiagram.Image.Clone();

            noiseMap = new PerlinNoise(99);
            heights = new double[OrderedPoints.Count];

            for (int i = 0; i < OrderedPoints.Count; i++)
            {
                var point = OrderedPoints[i];
                heights[i] = GetHeightAt(point[0], point[1]);
            }

            foreach (var province in Enumerable.Range(1, OrderedPoints.Count))
            {
                Map.TerrainTypes[province] = 0;
            }

            var seaProvinceCount = Convert.ToInt32(txtSea.Value) - 1;
            int c = 0;
            var index = Random.Next(OrderedPoints.Count) + 1;
            Map.TerrainTypes[index] |= TerrainFeatures.Sea;
            var seaPoints = new List<int>();
            while (c < seaProvinceCount)
            {
                if (OrderedPoints.Count > 75 && Random.NextDouble() < (75.0 / OrderedPoints.Count))
                {
                    var n = Map.Neighbours[index];
                    index = n.ElementAt(Random.Next(n.Count));
                    n = Map.Neighbours[index];
                    index = n.ElementAt(Random.Next(n.Count));
                }

                var potentialPoints = new List<int>();
                var neighbourIds = Map.Neighbours[index];
                foreach (var neighbourId in neighbourIds)
                {
                    if (!Map.TerrainTypes[neighbourId].HasFlag(TerrainFeatures.Sea))
                    {
                        var moreIds = Map.Neighbours[neighbourId];
                        potentialPoints.Add(neighbourId);
                        foreach (var nextId in moreIds)
                        {
                            if (Map.TerrainTypes[nextId].HasFlag(TerrainFeatures.Sea))
                            {
                                potentialPoints.Add(neighbourId);
                            }
                        }
                    }
                }
                int next = 1;
                if (potentialPoints.Count > 0)
                {
                    next = potentialPoints[Random.Next(potentialPoints.Count)];
                    Map.TerrainTypes[next] |= TerrainFeatures.Sea;
                    seaPoints.Add(next);
                    index = next;
                    c++;
                }
                else
                {
                    index = seaPoints[Random.Next(seaPoints.Count)];
                }
            }

            foreach (var seaPoint in seaPoints)
            {
                if (Map.Neighbours[seaPoint].All(id => Map.TerrainTypes[id].HasFlag(TerrainFeatures.Sea)))
                {
                    Map.TerrainTypes[seaPoint] |= TerrainFeatures.Deep;
                }
            }

            var orderedPoints = heights
                .Select((h, i) => new { height = h, index = i })
                .OrderBy(i => i.height);

            var edges = new List<Rectangle>();

            using (var g = Graphics.FromImage(picDiagram.Image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.DrawImageUnscaled(_jaggedImage, 0, 0);

                var borders = new List<Point[]>();

                foreach (var province in Map.TerrainTypes.Where(t => t.Value.HasFlag(TerrainFeatures.Sea)).Select(t => t.Key))
                {
                    var gp = new GraphicsPath();
                    var vectors = _provinces[OrderedPoints[province - 1]].ToArray();

                    //for (var i = 1; i <= vectors.Length; i++)
                    //{
                    //    var prev = vectors[i - 1].ToPointF();
                    //    var current = vectors[i % vectors.Length].ToPointF();

                    //    edges.Add(GetSegmentId(prev, current));
                    //}

                    //var jaggedPolygonPath = GetJaggedPolygonPath(vectors.Select(v => v.ToPoint()).ToList());
                    //gp.AddPolygon(jaggedPolygonPath);
                    
                    var points = new List<Point>();
                    var pointCount = vectors.Length;
                    for (int i = 1; i <= pointCount; i++)
                    {
                        var last = vectors[i - 1].ToPoint();
                        var current = vectors[i % pointCount].ToPoint();
                        var jaggedPoints = GetUnwrapedJaggedBorder(last, current);
                        borders.Add(jaggedPoints);
                        points.AddRange((jaggedPoints.First() != last) ? jaggedPoints.Reverse() : jaggedPoints);
                    }

                    gp.AddPolygon(points.ToArray());

                    using (var oceanFill = new PathGradientBrush(gp))
                    {
                        if (Map.TerrainTypes[province].HasFlag(TerrainFeatures.Deep))
                        {
                            oceanFill.CenterColor = Color.DodgerBlue;
                            oceanFill.SurroundColors = new[] { Color.DeepSkyBlue };
                        }
                        else
                        {
                            oceanFill.CenterColor = Color.LightSkyBlue;
                            oceanFill.SurroundColors = new[] { Color.LightBlue };
                        }

                        g.FillPath(oceanFill, gp);
                        g.Transform = _upMatrix;

                        g.FillPath(oceanFill, gp);
                        g.Transform = _downMatrix;

                        g.FillPath(oceanFill, gp);
                        g.Transform = _leftMatric;

                        g.FillPath(oceanFill, gp);
                        g.Transform = _rightMatrix;

                        g.FillPath(oceanFill, gp);
                        g.Transform = new Matrix();
                    }
                }

                foreach (var pointList in borders)
                {
                    g.Transform = new Matrix();
                    g.DrawCurve(_oceanBorder, pointList, 1.0f);
                    g.Transform = _upMatrix;
                    g.DrawCurve(_oceanBorder, pointList, 1.0f);
                    g.Transform = _downMatrix;
                    g.DrawCurve(_oceanBorder, pointList, 1.0f);
                    g.Transform = _leftMatric;
                    g.DrawCurve(_oceanBorder, pointList, 1.0f);
                    g.Transform = _rightMatrix;
                    g.DrawCurve(_oceanBorder, pointList, 1.0f);
                }

                var forestProvs = 0;
                var forestCount = Convert.ToInt32(txtForest.Value);
                while (forestProvs < forestCount)
                {
                    var provinceId = Random.Next(OrderedPoints.Count) + 1;

                    if (Map.TerrainTypes[provinceId].HasFlag(TerrainFeatures.Sea) || Map.TerrainTypes[provinceId].HasFlag(TerrainFeatures.Forest))
                        continue;

                    var poly = _provinces[OrderedPoints[provinceId - 1]];

                    Map.TerrainTypes[provinceId] |= TerrainFeatures.Forest;

                    var box = Box(poly.ToList());
                    for (var x = box.Item1[0] + 3; x < box.Item2[0]; x += Density)
                    {
                        for (var y = box.Item1[1] + 3; y < box.Item2[1]; y += Density)
                        {
                            if (!PointInPolygon(new Vector(x, y), poly.ToList(), 1.2f))
                                continue;
                            var image = _images["tree" + Random.Next(1, 5)];
                            var rx = (float)((Random.NextDouble() * Variance) - (Variance / 2));
                            var ry = (float)((Random.NextDouble() * Variance) - (Variance / 2));
                            var hx = image.Width / 2f;
                            var hy = image.Height / 2f;
                            g.DrawImage(image, (float)x + rx - hx, (float)y + ry - hy);
                            g.DrawImage(image, (float)x + rx - hx + Width(), (float)y + ry - hy);
                            g.DrawImage(image, (float)x + rx - hx - Width(), (float)y + ry - hy);
                            g.DrawImage(image, (float)x + rx - hx, (float)y + ry - hy + Height());
                            g.DrawImage(image, (float)x + rx - hx, (float)y + ry - hy - Height());
                        }
                    }

                    forestProvs++;
                }
            }

            var provinceAreas = Enumerable
                .Range(1, OrderedPoints.Count)
                .Select(i => new { ProvinceId = i, Area = CalculateProvinceArea(i)})
                .OrderBy(a => a.Area)
                .ToArray();

            var smallProvinces = provinceAreas.Take(OrderedPoints.Count/4);
            var largeProvinces = provinceAreas.Skip(OrderedPoints.Count*3/4);

            foreach (var smallProvince in smallProvinces)
                Map.TerrainTypes[smallProvince.ProvinceId] |= TerrainFeatures.Small;

            foreach (var largeProvince in largeProvinces)
                Map.TerrainTypes[largeProvince.ProvinceId] |= TerrainFeatures.Large;

            var bitmap = ((Bitmap)picDiagram.Image);
            foreach (var province in _provinces.Keys)
            {
                bitmap.SetPixel((int)province[0], (int)province[1], Color.White);
            }

            picDiagram.Invalidate();

            Map.MapImage = (Image)picDiagram.Image.Clone();

            cleanImage = null;
        }

        private float CalculateProvinceArea(int provinceId)
        {
            var xTotal = 0f;
            var yTotal = 0f;

            var provincePoint = OrderedPoints[provinceId - 1];
            var protinceBorderPoints = _provinces[provincePoint].ToArray();

            for (var i = 1; i <= protinceBorderPoints.Length; i++)
            {
                var prev = protinceBorderPoints[i - 1];
                var current = protinceBorderPoints[i % protinceBorderPoints.Length];

                xTotal += (float) prev[0] * (float) current[1];
                yTotal += (float) prev[1] * (float) current[0];
            }

            return Math.Abs(xTotal - yTotal)/2;
        }

        private Point[] GetJaggedPolygonPath(List<Point> polygon)
        {
            var points = new List<Point>();

            var last = polygon.Last();
            foreach (var point in polygon)
            {
                points.AddRange(GetJaggedPath(last, point).Skip(1));
                last = point;
            }

            return points.ToArray();
        }

        private IEnumerable<Point> GetJaggedPath(Point a, Point b)
        {
            var index = GetSegmentId(a, b);

            if (_indexToPath.ContainsKey(index))
            {
                var points = _indexToPath[index];
                return (points.First() != a) ? points.Reverse() : points;
            }

            var a1 = new PointF(a.X + Width(), a.Y);
            var b1 = new PointF(b.X + Width(), b.Y);
            index = GetSegmentId(a1, b1);
            if (_indexToPath.ContainsKey(index))
            {
                var points = _indexToPath[index];
                return (points.First() != a1) ? points.Reverse() : points;
            }

            a1 = new PointF(a.X - Width(), a.Y);
            b1 = new PointF(b.X - Width(), b.Y);
            index = GetSegmentId(a1, b1);
            if (_indexToPath.ContainsKey(index))
            {
                var points = _indexToPath[index];
                return (points.First() != a1) ? points.Reverse() : points;
            }

            a1 = new PointF(a.X, a.Y + Height());
            b1 = new PointF(b.X, b.Y + Height());
            index = GetSegmentId(a1, b1);
            if (_indexToPath.ContainsKey(index))
            {
                var points = _indexToPath[index];
                return (points.First() != a1) ? points.Reverse() : points;
            }

            a1 = new PointF(a.X, a.Y - Height());
            b1 = new PointF(b.X, b.Y - Height());
            index = GetSegmentId(a1, b1);
            if (_indexToPath.ContainsKey(index))
            {
                var points = _indexToPath[index];
                return (points.First() != a1) ? points.Reverse() : points;
            }

            return new[] { a, b };
        }

        private PerlinNoise noiseMap;
        private double GetHeightAt(double x, double y)
        {
            double v =
                // First octave
                (noiseMap.Noise(2 * x * Width(), 2 * y * Height(), -0.5) + 1) / 2 * 0.7 +
                // Second octave
                (noiseMap.Noise(4 * x * Width(), 4 * y * Height(), 0) + 1) / 2 * 0.2 +
                // Third octave
                (noiseMap.Noise(8 * x * Width(), 8 * y * Height(), +0.5) + 1) / 2 * 0.1;

            return Math.Min(1, Math.Max(0, v));
        }

        private Image cleanImage = null;
        private Rectangle prevBounds;

        private void DrawHighlightedProvince(int provinceId, int previousProvinceId = -1)
        {
            if (_provinces == null) return;
            var pen = new Pen(new SolidBrush(Color.FromArgb(100, 255, 255, 0)), 10);
            pen.CompoundArray = new[] { 0f, .2f, .6f, 1f };
            pen.Alignment = PenAlignment.Outset;
            pen.LineJoin = LineJoin.Round;
            var province = _provinces[OrderedPoints[provinceId - 1]].ToList();
            province.AddRange(province.ToList().Take(2));

            var points = new List<Point>();
            var pointCount = province.Count;
            for (int i = 1; i <= pointCount; i++)
            {
                var last = province[i - 1].ToPoint();
                var current = province[i % pointCount].ToPoint();
                var jaggedPoints = GetUnwrapedJaggedBorder(last, current);
                points.AddRange((jaggedPoints.First() != last) ? jaggedPoints.Reverse() : jaggedPoints);
            }

            using (var g = Graphics.FromImage(picDiagram.Image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                if (cleanImage == null)
                {
                    cleanImage = (Image)picDiagram.Image.Clone();
                }
                else if (previousProvinceId > 0)
                {
                    //g.DrawImage(cleanImage, 0, 0);
                    g.DrawImage(cleanImage, prevBounds, prevBounds, GraphicsUnit.Pixel);
                    //g.Transform = _upMatrix;
                    var newBounds = new Rectangle(prevBounds.Location, prevBounds.Size);
                    newBounds.Offset(Width(), 0);
                    g.DrawImage(cleanImage, newBounds, newBounds, GraphicsUnit.Pixel);
                    newBounds = new Rectangle(prevBounds.Location, prevBounds.Size);
                    newBounds.Offset(-1 * Width(), 0);
                    g.DrawImage(cleanImage, newBounds, newBounds, GraphicsUnit.Pixel);
                    newBounds = new Rectangle(prevBounds.Location, prevBounds.Size);
                    newBounds.Offset(0, Height());
                    g.DrawImage(cleanImage, newBounds, newBounds, GraphicsUnit.Pixel);
                    newBounds = new Rectangle(prevBounds.Location, prevBounds.Size);
                    newBounds.Offset(0, -1 * Height());
                    g.DrawImage(cleanImage, newBounds, newBounds, GraphicsUnit.Pixel);
                }

                g.Transform = new Matrix();
                g.DrawLines(pen, points.ToArray());
                g.Transform = _upMatrix;
                g.DrawLines(pen, points.ToArray());
                g.Transform = _downMatrix;
                g.DrawLines(pen, points.ToArray());
                g.Transform = _leftMatric;
                g.DrawLines(pen, points.ToArray());
                g.Transform = _rightMatrix;
                g.DrawLines(pen, points.ToArray());

                prevBounds = GetProvinceBoundingBox(points);
                prevBounds.Inflate(10, 10);
            }
            picDiagram.Invalidate();
        }

        private Point[] GetUnwrapedJaggedBorder(Point p1, Point p2)
        {
            var segmentId = GetSegmentId(p1, p2);
            var edge = _indexToPath[segmentId].ToList();
            var dx = Math.Min(p1.X, p2.X) - Math.Min(edge.First().X, edge.Last().X);
            var dy = Math.Min(p1.Y, p2.Y) - Math.Min(edge.First().Y, edge.Last().Y);
            var diff = new Size(dx, dy);
            var jaggedBorder = _indexToPath[GetSegmentId(p1, p2)].Select(pt => Point.Add(pt, diff)).ToArray();
            return jaggedBorder;
        }

        private Rectangle GetProvinceBoundingBox(ICollection<Point> points)
        {
            var xPoints = points.Select(p1 => p1.X).ToArray();
            var yPoints = points.Select(p1 => p1.Y).ToArray();
            var minX = xPoints.Min();
            var minY = yPoints.Min();
            var maxX = xPoints.Max();
            var maxY = yPoints.Max();
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        private int provinceIdHover;
        public Rectangle MapBounds;
        private PointF _borderPointX;
        private PointF _borderPointEdge;
        private PointF _borderPointY;

        private void picDiagram_MouseMove(object sender, MouseEventArgs e)
        {
            if (OrderedPoints == null) return;
            var fixedMouse = picDiagram.PointToClient(MousePosition);
            var provinceId = OrderedPoints
                .Select((p, i) => new { Distance = Distance(p, new Vector(new double[] { fixedMouse.X, fixedMouse.Y })), Index = i + 1 })
                .OrderBy(i => i.Distance)
                .First()
                .Index;

            if (provinceId != provinceIdHover)
            {
                DrawHighlightedProvince(provinceId, provinceIdHover);
                provinceIdHover = provinceId;
            }
        }

        private void btnGenerateTerrain_Click(object sender, EventArgs e)
        {
            SaveMapToDisk();
        }

        private void SaveMapToDisk()
        {
            btnSaveMap.Enabled = false;
            if (txtMapTitle.Text != "")
            {
                Map.Title = txtMapTitle.Text;
                Map.Filename = Map.Title;
                foreach (var invalidPathChar in Path.GetInvalidPathChars())
                {
                    Map.Filename = Map.Filename.Replace(invalidPathChar, '_');
                }
            }
            if (txtMapDesc.Text != "")
                Map.Description = txtMapDesc.Text;

            if (chkShowProvCount.Checked)
            {
                var totalProvinces = OrderedPoints.Count;
                var seaProvinces = txtSea.Value;
                var landProvinces = totalProvinces - seaProvinces;
                Map.Description = Map.Description +
                                  String.Format("{0}Land Provinces: {1}, Sea Provinces: {2}, Total Provinces: {3}",
                                                Environment.NewLine, landProvinces, seaProvinces, totalProvinces);
            }

            Map.GenerateMapToDisk();
            btnSaveMap.Enabled = true;
        }

        private void btnReadPretender_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            GenerateMapFromSeed();
        }

        private void fromSeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GenerateMapFromSeed();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveMapToDisk();
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            txtSeed.Value = new Random().Next(Int32.MaxValue);
            GenerateMapFromSeed();
        }
    }
}
