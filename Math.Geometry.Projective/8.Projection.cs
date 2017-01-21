using System;
using System.Text;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Geometry.Projective
{
    using System.Numerics;
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Complex;

    /// <summary>
    /// Map a range of real points with homogeneous coordinates onto the YZ-plane from a given center.
    /// </summary>
    public class Projection
    {
        /// <summary>
        /// Create a new projection from a center onto a plane.<para>
        /// The default center of projection is the point of infinity of the x-axis.</para><para>
        /// Optionally specify a real point as the center of projection.</para><para>
        /// The default plane of projection is the YZ-plane.</para><para>
        /// Optionally specify a real plane (not at infinity) on which to project.</para><para>
        /// The center must not lie in the plane of projection.</para>
        /// </summary>
        public Projection(Point3D projectionCenter = null, Plane3D projectionPlane = null)
        {
            if (projectionCenter != null)
            {
                if (!projectionCenter.IsReal()) throw new ArgumentException("center of projection is not real");
                ProjectionCenter = projectionCenter;
            }
            if (projectionPlane != null)
            {
                if (!projectionPlane.IsReal()) throw new ArgumentException("plane of projection is not real");
                ProjectionPlane = projectionPlane;
            }
            if (ProjectionPlane.Equals(Plane3D.Infinity))
            {
                throw new ArgumentException("the plane at infinity is not allowed as the plane of projection");
            }
            if (ProjectionPlane.IsIncident(projectionCenter))
            {
                throw new ArgumentException("the center of projection lies in the plane of projection");
            }

            // determine the 2-D origin in the projection plane
            if (ProjectionCenter.IsAtInfinity)
            {
                Origin = ProjectionCenter.Join(Point3D.Origin).Meet(ProjectionPlane);
            }
            else
            {
                Origin = ProjectionPlane.PerpendicularLine(ProjectionCenter).Meet(ProjectionPlane);
            }

            // determine an apropriate 2-D x-axis and x-unit-vector in the projection plane
            if (ProjectionPlane.IsIncident(Line3D.Yaxis))
            {
                XAxis = Line3D.Yaxis;
                XUnitVector = Vector3.EY;
            }
            else if (ProjectionPlane.IsIncident(Line3D.Zaxis))
            {
                XAxis = Line3D.Zaxis;
                XUnitVector = Vector3.EZ;
            }
            else if (ProjectionPlane.IsIncident(Line3D.Xaxis))
            {
                XAxis = Line3D.Xaxis;
                XUnitVector = Vector3.EX;
            }
            else
            {
                var point = ProjectionPlane.Meet(Line3D.Yaxis);
                if (point.IsAtInfinity)
                {
                    XAxis = Origin.Join(point);
                    XUnitVector = Vector3.EY;
                }
                else
                {
                    point = ProjectionPlane.Meet(Line3D.Zaxis);
                    if (point.IsAtInfinity)
                    {
                        XAxis = Origin.Join(point);
                        XUnitVector = Vector3.EZ;
                    }
                    else
                    {
                        point = ProjectionPlane.Meet(Line3D.Xaxis);
                        if (point.IsAtInfinity)
                        {
                            XAxis = Origin.Join(point);
                            XUnitVector = Vector3.EX;
                        }
                        else
                        {
                            var helpline = ProjectionCenter.Join(Line3D.Yaxis).Meet(ProjectionPlane);
                            var helppoint = ProjectionCenter.Join(Point3D.UnityY).Meet(ProjectionPlane);
                            XAxis = Origin.Join(helpline.Meet(Plane3D.Infinity));
                            XUnitVector = (helppoint.ToAffine() - Origin.ToAffine()).Normalize();
                        }
                    }
                }
            }

            // determine an apropriate 2-D y-axis and y-unit-vector in the projection plane
            YUnitVector = ProjectionPlane.NormalVector.CrossProduct(XUnitVector).Normalize();
            YAxis = Origin.Join(new Point3D(Origin.ToAffine() + YUnitVector));

            OriginAffine = Origin.ToAffine();
            if (ProjectionCenter.IsAtInfinity)
            {
                ProjectionCenterAffine = ProjectionCenter.AsDirection();
            }
            else
            {
                ProjectionCenterAffine = ProjectionCenter.ToAffine();
            }
        }

        /// <summary>
        /// The center of the spatial projection.
        /// </summary>
        public Point3D ProjectionCenter = Point3D.InfinityX;
        Vector3 ProjectionCenterAffine = Point3D.InfinityX.ToAffine();
        /// <summary>
        /// The plane onto which the projection takes place.
        /// </summary>
        public Plane3D ProjectionPlane = Plane3D.YZ;
        /// <summary>
        /// The origin of the two-dimensional coordinate system in the plane of projection.
        /// </summary>
        public Point3D Origin = Point3D.Origin;
        Vector3 OriginAffine = Point3D.Origin.ToAffine();
        /// <summary>
        /// The x-axis of the two-dimensional coordinate system in the plane of projection.
        /// </summary>
        public Line3D XAxis = Line3D.Yaxis;
        /// <summary>
        /// The 3-D unit vector along the x-axis of the two-dimensional coordinate system in the plane of projection.
        /// </summary>
        public Vector3 XUnitVector = Vector3.EY;
        /// <summary>
        /// The y-axis of the two-dimensional coordinate system in the plane of projection.
        /// </summary>
        public Line3D YAxis = Line3D.Zaxis;
        /// <summary>
        /// The 3-D unit vector along the y-axis of the two-dimensional coordinate system in the plane of projection.
        /// </summary>
        public Vector3 YUnitVector = Vector3.EZ;

        /// <summary>
        /// Project a range of points.
        /// </summary>
        public List<Vector2> Project(List<Point3D> points, Vector2 offset = null)
        {
            var rv = new List<Vector2>();

            if (offset == null) offset = new Vector2(0, 0);

            List<ProjectionSet> projectedsets = RealProjectedPoints(points);

            foreach (var projectedset in projectedsets.Select(p => p.ProjectedPoint))
            {
                Vector3 affineprojectedvector = projectedset.ToAffine() - OriginAffine;

                Complex x = affineprojectedvector * XUnitVector;
                Complex y = affineprojectedvector * YUnitVector;

                Vector2 drawingpoint = offset + new Vector2(x.Real, y.Real);

                drawingpoint.Name = projectedset.Name;

                rv.Add(drawingpoint);
            }

            return rv;
        }

        /// <summary>
        /// Project a range of points using the type of projection (frontside, backside, behindside).<para>
        /// The lists that are returned are grouped in continuous ranges of points each of a certain type of projection.</para>
        /// </summary>
        public List<ProjectionList> Project(ParameterList<Point3D> pointsparameters, Vector2 offset = null)
        {
            var rv = new List<ProjectionList>();

            if (pointsparameters.Count == 0) return rv;

            if (offset == null) offset = new Vector2(0, 0);

            List<Point3D> points = pointsparameters.Values.ToList();

            List<ProjectionSet> projectionsets = RealProjectedPoints(points);

            bool ready = false;
            int startindex = 0;

            while (!ready)
            {
                var liststoprocess = new List<ProjectionList>();

                for (int i = startindex; i < projectionsets.Count; i++)
                {
                    var projectiontypes = Extensions.GetFlags(projectionsets[i].ProjectionType);

                    foreach (ProjectionType item in projectiontypes)
                    {
                        if (!rv.Any(p => p.ProjectionType == item && p.Values.Contains(projectionsets[i].ProjectedPoint2D)))
                        {
                            var list = new ProjectionList(item);
                            liststoprocess.Add(list);
                            rv.Add(list);
                        }
                    }

                    if (liststoprocess.Any())
                    {
                        break;
                    }
                    else
                    {
                        startindex = i + 1;
                    }
                }

                if (liststoprocess.Any())
                {
                    foreach (var list in liststoprocess)
                    {
                        for (int i = 0; i < projectionsets.Count; i++)
                        {
                            if (projectionsets[i].ProjectionType.HasFlag(list.ProjectionType))
                            {
                                addvector2(projectionsets[i], offset, list);
                            }

                            else
                            {
                                break;
                            }
                        }
                    }
                }

                else
                {
                    ready = true;
                }
            }

            return rv;
        }

        void addvector2(ProjectionSet projectionset, Vector2 offset, ProjectionList list)
        {
            Point3D projectedpoint = projectionset.ProjectedPoint;

            Vector3 affineprojectedvector = projectedpoint.ToAffine() - OriginAffine;

            Complex x = affineprojectedvector * XUnitVector;
            Complex y = affineprojectedvector * YUnitVector;

            Vector2 drawingpoint = offset + new Vector2(x.Real, y.Real);

            drawingpoint.Name = projectedpoint.Name;

            projectionset.ProjectedPoint2D = drawingpoint;

            list.Values.Add(drawingpoint);
        }

        /// <summary>
        /// An accurate approximation of the points where the list of projected points meets the projection plane is added.
        /// </summary>
        List<ProjectionSet> RealProjectedPoints(ParameterList<Point3D> spatialpoints)
        {
            var rv = new List<ProjectionSet>();

            for (int i = 0; i < spatialpoints.Count; i++)
            {
                var spatialpoint = spatialpoints.Values[i];

                var currentset = RealProjectedPoint(spatialpoint);

                if (currentset != null)
                {
                    var previousset = rv.LastOrDefault();

                    if (previousset != null)
                    {
                        if ((previousset.ProjectionType == ProjectionType.Frontside && currentset.ProjectionType == ProjectionType.Backside) ||
                            (previousset.ProjectionType == ProjectionType.Backside && currentset.ProjectionType == ProjectionType.Frontside))
                        {
                            var previousspatialpoint = spatialpoints.ValuesAndParameters.FirstOrDefault(p => p.Item2 == previousset.SpatialPoint);
                            var currentspatialpoint = spatialpoints.ValuesAndParameters[i];

                            if (previousspatialpoint != null && currentspatialpoint != null && previousspatialpoint != null)
                            {
                                // insert a point in the middle, where the curve will cut the projection plane

                                ProjectionSet frontside = previousset.ProjectionType == ProjectionType.Frontside ? previousset : currentset;
                                ProjectionSet backside = previousset.ProjectionType == ProjectionType.Backside ? previousset : currentset;
                                ProjectionSet middle = null;

                                for (int j = 0; j < 10; j++)
                                {
                                    Complex t = (previousspatialpoint.Item1.Value + currentspatialpoint.Item1.Value) / 2;
                                    Point3D middleSpatial = spatialpoints.Function(t);
                                    middle = RealProjectedPoint(middleSpatial);

                                    if (middle == null)
                                    {
                                        break;
                                    }
                                    else if (middle.ProjectionType.HasFlag(ProjectionType.Frontside) && middle.ProjectionType.HasFlag(ProjectionType.Backside))
                                    {
                                        break;
                                    }
                                    else if (middle.ProjectionType.HasFlag(ProjectionType.Frontside))
                                    {
                                        frontside = middle;
                                    }
                                    else if (middle.ProjectionType.HasFlag(ProjectionType.Backside))
                                    {
                                        backside = middle;
                                    }
                                    else
                                    {
                                        middle = null;
                                        break;
                                    }
                                }

                                if (middle != null && middle.ProjectionType.HasFlag(ProjectionType.Frontside) &&
                                                      middle.ProjectionType.HasFlag(ProjectionType.Backside))
                                {
                                    rv.Add(middle);
                                }
                                else
                                {
                                    addmiddle(frontside, backside, rv);
                                }
                            }

                            else
                            {
                                // insert a point in the middle, where the curve will cut the projection plane
                                addmiddle(previousset, currentset, rv);
                            }
                        }
                    }

                    rv.Add(currentset);
                }
            }

            return rv;
        }

        /// <summary>
        /// An approximation of the points where the list of projected points meets the projection plane is added.<para>
        /// If applicable these extra points can be removed: their 'spatial point' property is empty (null).</para>
        /// </summary>
        List<ProjectionSet> RealProjectedPoints(List<Point3D> spatialpoints)
        {
            var rv = new List<ProjectionSet>();

            for (int i = 0; i < spatialpoints.Count; i++)
            {
                ProjectionSet currentset = RealProjectedPoint(spatialpoints[i]);

                if (currentset != null)
                {
                    var previousset = rv.LastOrDefault();

                    if (previousset != null)
                    {
                        if ((previousset.ProjectionType == ProjectionType.Frontside && currentset.ProjectionType == ProjectionType.Backside) ||
                            (previousset.ProjectionType == ProjectionType.Backside && currentset.ProjectionType == ProjectionType.Frontside))
                        {
                            // insert a point in the middle as the point where the curve will cut the projection plane
                            addmiddle(previousset, currentset, rv);
                        }
                    }

                    rv.Add(currentset);
                }
            }

            return rv;
        }

        void addmiddle(ProjectionSet point1, ProjectionSet point2, List<ProjectionSet> list)
        {
            // TODO which one of the following tow methods is the best way to add the 'middle' point?
            var middle1 = point1.SpatialPoint.Join(point2.SpatialPoint).Meet(ProjectionPlane);
            var middle2 = new Point3D((point1.ProjectedPoint.ToAffine() + point2.ProjectedPoint.ToAffine()) * 0.5);

            list.Add(new ProjectionSet
            {
                SpatialPoint = null,
                ProjectedPoint = middle1,
                ProjectionType = ProjectionType.Backside | ProjectionType.Frontside
            });

            list.Add(new ProjectionSet
            {
                SpatialPoint = null,
                ProjectedPoint = middle2,
                ProjectionType = ProjectionType.Backside | ProjectionType.Frontside
            });
        }

        ProjectionSet RealProjectedPoint(Point3D spatialpoint)
        {
            if (!spatialpoint.IsReal()) return null;

            Line3D ray = spatialpoint.Join(ProjectionCenter);
            if (ray == null) return null;

            Point3D projectedpoint = ray.Meet(ProjectionPlane);

            projectedpoint.Name = spatialpoint.Name;

            Complex? factor = null;

            if (ProjectionCenter.IsAtInfinity)
            {
                // vector1 = vector from projected point to spatial point
                Vector3 vector1 = spatialpoint.ToAffine() - projectedpoint.ToAffine();

                // vector2 = directional vector of the projection-center 
                Vector3 vector2 = ProjectionCenterAffine;

                if (vector1.IsZero())
                {
                    factor = Complex.Zero;
                }

                else
                {
                    // vector1 = factor * vector2
                    factor = Extensions.LinearDependentFactor(vector2, vector1);
                }
            }

            else
            {
                // vector1 = vector from projection-center to spatial point
                Vector3 vector1 = spatialpoint.ToAffine() - ProjectionCenterAffine;

                // vector2 = vector from projection-center to projected point
                Vector3 vector2 = projectedpoint.ToAffine() - ProjectionCenterAffine;

                // vector1 = factor * vector2
                factor = Extensions.LinearDependentFactor(vector2, vector1);
            }

            if (factor == null)
            {
                throw new AlgorithmException("Unexpected: two vectors are not linearly dependent");
            }
            if (!factor.Value.Imaginary.IsZero())
            {
                throw new AlgorithmException("Unexpected: linearly dependency factor is not a real number");
            }

            ProjectionType? projectiontype = null;

            if (ProjectionCenter.IsAtInfinity)
            {
                if (factor.Value.Real >= -Extensions.PrecisionZero)
                {
                    if (projectiontype == null)
                    {
                        projectiontype = ProjectionType.Frontside;
                    }
                    else
                    {
                        projectiontype |= ProjectionType.Frontside;
                    }
                }
                if (factor.Value.Real <= Extensions.PrecisionZero)
                {
                    if (projectiontype == null)
                    {
                        projectiontype = ProjectionType.Backside;
                    }
                    else
                    {
                        projectiontype |= ProjectionType.Backside;
                    }
                }
            }

            else
            {
                if (factor.Value.Real >= -Extensions.PrecisionZero && factor.Value.Real <= 1 + Extensions.PrecisionZero)
                {
                    if (projectiontype == null)
                    {
                        projectiontype = ProjectionType.Frontside;
                    }
                    else
                    {
                        projectiontype |= ProjectionType.Frontside;
                    }
                }
                if (factor.Value.Real >= 1 - Extensions.PrecisionZero)
                {
                    if (projectiontype == null)
                    {
                        projectiontype = ProjectionType.Backside;
                    }
                    else
                    {
                        projectiontype |= ProjectionType.Backside;
                    }
                }
                if (factor.Value.Real <= Extensions.PrecisionZero)
                {
                    if (projectiontype == null)
                    {
                        projectiontype = ProjectionType.Behindside;
                    }
                    else
                    {
                        projectiontype |= ProjectionType.Behindside;
                    }
                }
            }

            if (projectiontype == null)
            {
                return null;
            }

            return new ProjectionSet { SpatialPoint = spatialpoint, ProjectedPoint = projectedpoint, ProjectionType = projectiontype.Value };
        }

        public List<ProjectionSet> projectiondsets { get; set; }

        public List<ProjectionSet> projectionsets { get; set; }
    }

    /// <summary>
    /// The set of a spatial point, its type of projection, its projected point as a three-dimensional point and with its the two-dimensional coordinates.
    /// </summary>
    public class ProjectionSet
    {
        /// <summary>
        /// 
        /// </summary>
        public Point3D SpatialPoint { get; set; }
        /// <summary>
        /// The projected point in the projection plane.
        /// </summary>
        public Point3D ProjectedPoint { get; set; }
        /// <summary>
        /// The two-dimensional coordinates of the projected point in the projection plane.
        /// </summary>
        public Vector2 ProjectedPoint2D { get; set; }
        /// <summary>
        /// The type of projection (frontside, backside, behindside).
        /// </summary>
        public ProjectionType ProjectionType { get; set; }
    }

    /// <summary>
    /// Indicates the position of the spatial point with respect to the projection plane and the projection center.
    /// </summary>
    [Flags]
    public enum ProjectionType
    {
        /// <summary>
        /// The point is between the projection center and the projection plane.
        /// </summary>
        Frontside = 1,
        /// <summary>
        /// The point is behind the projection plane seen from the projection center.
        /// </summary>
        Backside = 2,
        /// <summary>
        /// The points is behind the projection center seen from the projection plane.
        /// </summary>       
        Behindside = 4,
    }
}
