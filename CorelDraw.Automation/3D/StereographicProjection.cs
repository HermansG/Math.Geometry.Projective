using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CorelDraw.Automation.Drawings
{
    using System.Numerics;
    using MathNet.Numerics;
    using Geometry.Projective;
    using MathNet.Numerics.LinearAlgebra.Complex;

    [Draw(false, PageOrientation = VGCore.cdrPageOrientation.cdrPortrait)]
    public class StereographicProjection : DrawingBase
    {
        public StereographicProjection(VGCore.Document document, VGCore.Page page) : base(document, page) { }

        public override void CreateDrawing()
        {
            // a focal distance of 60 / Math.Sqrt(2) makes that the spherical lemniscate fills half of the sphere
            ParameterList<Point2D> lemniscate_nodes2d = Functions.NodesLemniscate(focal_distance: 60 / Math.Sqrt(2), rotationangle: 90);

            // put all 2d nodes in the 3d XY-plane
            ParameterList<Point3D> lemniscate_nodes3d = lemniscate_nodes2d.Chain<Point3D>(p => new Point3D(p));

            double radius = 30;
            Point3D NorthPole = new Point3D(1, 0, 0, 2 * radius);
            NorthPole.Name = "NorthPole";
            Point3D Center = new Point3D(1, 0, 0, radius);
            Center.Name = "Center";

            // put all 3d nodes in the XY-plane onto the sphere
            ParameterList<Point3D> lemniscate_stereographically_projected = lemniscate_nodes3d.Chain<Point3D>(p => ProjectStereographically(p, NorthPole));

            Point3D ProjectionCenter = new Point3D(0, 1, 0, 0.5);
            Correlation polarity = Correlation.CreatePolaritySphere(Center, radius);
            Plane3D ProjectionPlane = polarity.Map(ProjectionCenter);
            Projection projection = new Projection(ProjectionCenter, ProjectionPlane);

            List<ProjectionList> spherical_lemniscate_drawing_nodes = projection.Project(lemniscate_stereographically_projected);

            foreach (var list in spherical_lemniscate_drawing_nodes)
            {
                CreateCurve(list.Values.Select(n => n + CenterOfPaper).ToList(), closed: false, name: "Spherical lemniscate frontside");
            }

            spherical_lemniscate_drawing_nodes = projection.Project(lemniscate_stereographically_projected);

            foreach (var list in spherical_lemniscate_drawing_nodes)
            {
                CreateCurve(list.Values.Select(n => n + CenterOfPaper).ToList(), closed: false, name: "Spherical lemniscate backside");
            }

            // the projected center of the sphere
            Point3D center_frontcircle = ProjectionCenter.Join(Center).Meet(ProjectionPlane);
            // the radius of the front circle in the ptojection plane (Phytagoras' theorem)
            double distance = center_frontcircle.ToAffine().Distance(Center.ToAffine()).Real;
            double radius_frontcircle = Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(distance, 2));
            var frontcircle_projectionplane = Functions.NodesCircle(center_frontcircle, ProjectionPlane, radius_frontcircle).Values;
            var frontcircle_drawingnodes = projection.Project(
                                frontcircle_projectionplane, CenterOfPaper);
            CreateCurve(frontcircle_drawingnodes, closed: true, name: "FrontCircle");

            var points = new List<Point3D>();
            points.Add(Point3D.Origin);
            Point3D.Origin.Name = "Origin";
            points.Add(Center);
            points.Add(NorthPole);
            List<Vector2> point_nodes = projection.Project(points);
            foreach (var point in point_nodes)
            {
                CreatePoint(point + CenterOfPaper, 0.1, Orange, name: point.Name);
            }

            // great circles (meridians) of the sphere
            for (int alpha = 5; alpha < 185; alpha += 15)
            {
                double radians = Trig.DegreeToRadian(alpha);
                var plane = new Plane3D(0, Math.Cos(radians), Math.Sin(radians), 0);
                var greatcircle = Functions.NodesCircle(Center, plane, radius);
                List<Vector2> greatcircle_drawingnodes = null; //  projection.Project(greatcircle, CenterOfPaper, ProjectionType.Frontside);
                CreateCurve(greatcircle_drawingnodes, closed: false, name: "GreatCircle" + alpha);
            }

            // lines of latitude (parallels of the equator) of the sphere
            for (int alpha = -60; alpha <= 60; alpha += 30)
            {
                double radians = Trig.DegreeToRadian(alpha);
                double height = radius * (1 - Math.Sin(radians));
                var plane = new Plane3D(-height, 0, 0, 1);
                Point3D center = new Point3D(1, 0, 0, height);
                var latitudecircle = Functions.NodesCircle(center, plane, radius * Math.Cos(radians));
                List<Vector2> latitudecircle_drawingnodes = null; // projection.Project(latitudecircle, CenterOfPaper, ProjectionType.Frontside);
                CreateCurve(latitudecircle_drawingnodes, closed: false, name: "LatitudeCircle" + alpha);
            }
        }

        Point3D ProjectStereographically(Point3D node_3d, Point3D NorthPole)
        {
            // instead of using a formula to calculate the stereographic projection on the sphere
            // we use a geometrical method, also to test the Projective.Geometry library

            // the lines from the north pole to the points of nodes_3d that are assumed to be in the XY-plane
            Line3D line = NorthPole.Join(node_3d);

            // in case of the north pole itself
            if (line == null) return null;

            if (line.IsIncident(Point3D.Origin))
            {
                return Point3D.Origin;
            }

            // the plane perpendicular to 'line', through the origin
            Plane3D plane1 = new Plane3D(line.DirectionVector);
            // the plane through 'line' and the origin
            Plane3D plane2 = line.Join(Point3D.Origin);
            // the common line of these two planes
            Line3D other = plane1.Meet(plane2);
            // 'line' meets this other line in the projection point on the sphere (Thales' theorem)
            Point3D stereographic_projection = line.Meet(other);

            return stereographic_projection;
        }
    }
}
