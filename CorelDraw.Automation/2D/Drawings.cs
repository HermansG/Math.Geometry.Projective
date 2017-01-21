using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CorelDraw.Automation
{
    using System.Numerics;
    using MathNet.Numerics;
    using Geometry.Projective;
    using MathNet.Numerics.LinearAlgebra.Complex;

    [Draw(false, PageOrientation = VGCore.cdrPageOrientation.cdrLandscape)]
    public class LemniscateBernoulli : DrawingBase
    {
        public LemniscateBernoulli(VGCore.Document document, VGCore.Page page) : base(document, page) { }

        public override void CreateDrawing()
        {
            ParameterList<Point2D> nodes_homogeneous = Functions.NodesLemniscate(focal_distance: 30, center: CenterOfPaper, rotationangle: 45);

            var nodes = new List<Vector2>();
            foreach (var item in nodes_homogeneous.Values)
            {
                nodes.Add(item.ToAffine());
            }

            var shape = CreateCurve(nodes, closed: true, name: "Lemniscate of Bernoulli 45");

            nodes_homogeneous = Functions.NodesLemniscate(focal_distance: 30, center: CenterOfPaper, rotationangle: 0);

            nodes = new List<Vector2>();
            foreach (var item in nodes_homogeneous.Values)
            {
                nodes.Add(item.ToAffine());
            }

            shape = CreateCurve(nodes, closed: true, name: "Lemniscate of Bernoulli 0");

            nodes_homogeneous = Functions.NodesLemniscate(focal_distance: 30, center: CenterOfPaper, rotationangle: 90);

            nodes = new List<Vector2>();
            foreach (var item in nodes_homogeneous.Values)
            {
                nodes.Add(item.ToAffine());
            }

            shape = CreateCurve(nodes, closed: true, name: "Lemniscate of Bernoulli 90");
        }
    }

    [Draw(false, PageOrientation = VGCore.cdrPageOrientation.cdrPortrait)]
    public class OrthogonalHyperbola : DrawingBase
    {
        public OrthogonalHyperbola(VGCore.Document document, VGCore.Page page) : base(document, page) { }

        public override void CreateDrawing()
        {
            Func<double, double> Step = t => Math.Abs(t) > 1 ? 0.1 : 0.01;

            var nodes = new List<Vector2>();

            for (double t = -10; 1 / t >= -10; t += Step(t))
            {
                var node = new Vector2(t, 1 / t) + CenterOfPaper;
                nodes.Add(node);
            }

            var shape1 = CreateCurve(nodes, closed: false);

            nodes.Clear();
            for (double t = 10; 1 / t <= 10; t -= Step(t))
            {
                var node = new Vector2(t, 1 / t) + CenterOfPaper;
                nodes.Add(node);
            }

            var shape2 = CreateCurve(nodes, closed: false);

            var shaperange = new VGCore.ShapeRange();
            shaperange.Add(shape1);
            shaperange.Add(shape2);
            var shape = shaperange.Combine();

            shape.Name = "Orthogonal hyperbola";
        }
    }

    [Draw(false, PageOrientation = VGCore.cdrPageOrientation.cdrLandscape)]
    public class LemniscatesTransformed : DrawingBase
    {
        public LemniscatesTransformed(VGCore.Document document, VGCore.Page page) : base(document, page) { }

        public override void CreateDrawing()
        {
            double focal_distance = 30;
            ParameterList<Point2D> nodes_homogeneous = Functions.NodesLemniscate(focal_distance);

            // DenseMatrix matrix = new DenseMatrix(3);
            // matrix.SetColumn(0, new Complex[] { 1, 0, 0 });
            // matrix.SetColumn(1, new Complex[] { 0, 4, 3 });
            // matrix.SetColumn(2, new Complex[] { 0, 1, 2 });
            // Collineation2D transformation = new Collineation2D(matrix, CoordinateType.Pointwise);

            // using 50 millimeters the lemniscate will pass through infinity
            var infinities = new List<double> { 1e9, 150, 100, 80, 50, 20 };

            foreach (var infinity in infinities)
            {
                var preimages = new List<Point2D> { Point2D.Origin, Point2D.InfinityX, Point2D.InfinityY, Point2D.Unity };
                var images = new List<Point2D> { Point2D.Origin, new Point2D(1, infinity, 0), new Point2D(1, 0, infinity), Point2D.Unity };

                Collineation2D transformation = new Collineation2D(preimages, images);

                var nodes_transformed = transformation.Map(nodes_homogeneous.Values).Select(n => n.ToAffine()).ToList();

                var nodes = new List<List<Vector2>>();
                nodes.Add(new List<Vector2>());
                int range = 0;
                for (int i = 0; i < nodes_transformed.Count; i++)
                {
                    bool throughinfinity = false;
                    if (i > 1)
                    {
                        if (nodes_transformed[i].Norm() > 0.5 * HalfPaperWidth &&
                            nodes_transformed[i - 1].Norm() > nodes_transformed[i - 2].Norm() &&
                            Math.Sign(nodes_transformed[i][0].Real) != Math.Sign(nodes_transformed[i - 1][0].Real))
                        {
                            throughinfinity = true;
                        }
                    }
                    if (throughinfinity)
                    {
                        nodes.Add(new List<Vector2>());
                        range++;
                        if (nodes_transformed[i].Norm() < HalfPaperWidth)
                        {
                            nodes[range].Add(nodes_transformed[i] + CenterOfPaper);
                        }
                    }
                    else if (nodes_transformed[i].Norm() < HalfPaperWidth)
                    {
                        nodes[range].Add(nodes_transformed[i] + CenterOfPaper);
                    }
                }

                if (nodes.Count > 1)
                {
                    var shape = CreateCurve(nodes);
                    shape.Name = "Bernoulli lemniscate proj. tr. (" + infinity + ")";
                }
                else
                {
                    var shape = CreateCurve(nodes[0], closed: true, name: "Lemniscate of Bernoulli proj. tr. (" + infinity + ")");
                }
            }
        }
    }

    [Draw(false, PageOrientation = VGCore.cdrPageOrientation.cdrLandscape)]
    public class MoebiusGrid : DrawingBase
    {
        public MoebiusGrid(VGCore.Document document, VGCore.Page page) : base(document, page) { }

        public override void CreateDrawing()
        {
            var origin = new Complex(60, 60);
            var unity = new Complex(60, 0);
            var infinity = new Complex(60, -60);

            MoebiusPlane complexplane = new MoebiusPlane(origin, unity, infinity);

            for (double r = 0.5; r < 10; r *= 1.5)
            {
                var circle = complexplane.CircleApollonius(r);

                if (circle.IsLine)
                {
                    var toexclude = new List<Point2D> { circle.Line.Meet(Line2D.Infinity) };
                    var point1 = circle.Line.GetPoint(exclude: toexclude);
                    toexclude.Add(point1);
                    var point2 = circle.Line.GetPoint(exclude: toexclude);
                    var shape = CreateLine(point1.ToAffine() + CenterOfPaper, point2.ToAffine() + CenterOfPaper,
                        name: "Apollonius r=" + r.ToString("N1"));
                }
                else
                {
                    var shape = CreateCircle(circle.Center + CenterOfPaper, circle.Radius, name: "Apollonius r=" + r.ToString("N1"));
                }
            }

            double delta = Math.PI / 20;
            for (double phi = 0; phi < 0; phi += delta)
            //for (double phi = 0; phi < Math.PI; phi += delta)
            {
                var circle = complexplane.CircleCoaxial(phi);

                if (circle.IsLine)
                {
                    var toexclude = new List<Point2D> { circle.Line.Meet(Line2D.Infinity) };
                    var point1 = circle.Line.GetPoint(exclude: toexclude);
                    toexclude.Add(point1);
                    var point2 = circle.Line.GetPoint(exclude: toexclude);
                    var shape = CreateLine(point1.ToAffine() + CenterOfPaper, point2.ToAffine() + CenterOfPaper,
                        name: "Coaxial phi=" + phi.ToString("N1"));
                }
                else
                {
                    var shape = CreateCircle(circle.Center + CenterOfPaper, circle.Radius, name: "Coaxial phi=" + phi.ToString("N1"));
                }
            }
        }
    }
}
