using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace CorelDraw.Automation
{
    using VGCore;
    using Geometry.Projective;

    public abstract class DrawingBase
    {
        public DrawingBase(Document document, Page page)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (page == null) throw new ArgumentNullException("page");

            Document = document;
            Page = page;

            var attribute = this.GetType().GetCustomAttribute<DrawAttribute>();
            if (attribute != null)
            {
                Page.Orientation = attribute.PageOrientation;
            }

            // When you put an object fully outside the page borders, you are moving it onto the 'Desktop' master layer
            // If any part of the object gets inside the borders of a Page, CorelDRAW will add that object to the top layer on that Page

            Layer = Page.ActiveLayer;
            Layer.Name = Page.Name;
            Layer.Visible = true;
            Layer.Editable = true;
        }

        public double HalfPaperWidth { get { return (Page.SizeWidth / 2); } }
        public double HalfPaperHeight { get { return (Page.SizeHeight / 2); } }
        /// <summary>
        /// Nota bene: the origin X = 0, Y = 0 is always the BottomLeft corner of the ActivePage.
        /// </summary>
        public VectorC2 CenterOfPaper { get { return new VectorC2(Page.SizeWidth / 2, Page.SizeHeight / 2); } }
        public VectorC2 TopLeft { get { return new VectorC2(CenterOfPaper[0] - HalfPaperWidth, CenterOfPaper[1] + HalfPaperHeight); } }
        public VectorC2 TopRight { get { return new VectorC2(CenterOfPaper[0] + HalfPaperWidth, CenterOfPaper[1] + HalfPaperHeight); } }
        public VectorC2 BottomLeft { get { return new VectorC2(CenterOfPaper[0] - HalfPaperWidth, CenterOfPaper[1] - HalfPaperHeight); } }
        public VectorC2 BottomRight { get { return new VectorC2(CenterOfPaper[0] + HalfPaperWidth, CenterOfPaper[1] - HalfPaperHeight); } }
        public Document Document { get; private set; }
        public Page Page { get; private set; }
        public Layer Layer { get; private set; }

        /// <summary>
        /// The actual drawing in CorelDraw created by using automation.
        /// </summary>
        public abstract void CreateDrawing();

        public Color Orange { get { return GetColor(0, 60, 100, 0); } }
        public Color GetColor(int cyan = 0, int magenta = 0, int yellow = 0, int black = 100)
        {
            return Layer.Application.CreateCMYKColor(cyan, magenta, yellow, black);
        }

        public ArrowHead GetArrowHead(int index = 0)
        {
            return Layer.Application.ArrowHeads[index];
        }

        public void SetStandardProperties(Shape shape, Color color = null)
        {
            shape.Fill.ApplyNoFill();
            shape.Outline.SetPropertiesEx(Width: 0.02,
                                           Style: null,
                                           Color: color == null ? GetColor() : color,
                                           StartArrow: GetArrowHead(),
                                           EndArrow: GetArrowHead(),
                                           BehindFill: cdrTriState.cdrFalse,
                                           ScaleWithShape: cdrTriState.cdrFalse,
                                           LineCaps: cdrOutlineLineCaps.cdrOutlineButtLineCaps,
                                           LineJoin: cdrOutlineLineJoin.cdrOutlineMiterLineJoin,
                                           NibAngle: 0d,
                                           NibStretch: 100,
                                           DashDotLength: 5d,
                                           PenWidth: 1.0,
                                           MiterLimit: 0.0,
                                           Justification: cdrOutlineJustification.cdrOutlineJustificationMiddle);
        }

        Dictionary<string, Shape> points = new Dictionary<string, Shape>();
        public Shape CreatePoint(VectorC2 center, double radius = 0.3, Color color = null, string name = null)
        {
            Shape point = null;
            string key = radius.ToString("#.#####") + "|";
            if (color != null)
            {
                key += color.CMYKCyan + "|" + color.CMYKMagenta + "|" + color.CMYKYellow + "|" + color.CMYKBlack + "|";
            }
            if (points.TryGetValue(key, out point))
            {
                point = point.Duplicate(center[0].Real - point.CenterX, center[1].Real - point.CenterY);
            }
            else
            {
                point = Layer.CreateEllipse2(center[0].Real, center[1].Real, radius, radius);
                SetStandardProperties(point, color);
                if (color == null)
                {
                    point.Fill.ApplyUniformFill(GetColor(0, 0, 0, 100));
                }
                else
                {
                    point.Fill.ApplyUniformFill(color);
                }
                points.Add(key, point);
            }

            if (name != null) point.Name = name;

            return point;
        }

        public Shape CreateCircle(VectorC2 center, double radius, string name = null)
        {
            return CreateEllipse(center, radius, radius, name);
        }

        public Shape CreateEllipse(VectorC2 center, double radius_horizontal, double radius_vertical, string name = null)
        {
            var shape = Layer.CreateEllipse2(center[0].Real, center[1].Real, radius_horizontal, radius_vertical);

            SetStandardProperties(shape);

            if (name != null) shape.Name = name;

            return shape;
        }

        public Shape CreateRectangle(VectorC2 center, double width, double height, string name = null)
        {
            // Documentation states that the first 2 parameters x, y are the UL corner; if I am not mistaken they are the BL corner
            var shape = Layer.CreateRectangle2(center[0].Real - 0.5 * width, center[1].Real - 0.5 * height, width, height);

            SetStandardProperties(shape);

            if (name != null) shape.Name = name;

            return shape;
        }

        public Shape CreatePolyLine(List<VectorC2> nodes, bool closed = false, string name = null)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            if (nodes.Count < 2) throw new ArgumentException("minimum of 2 nodes required");

            var curve = new Curve();
            var subpath = curve.CreateSubPath(nodes[0][0].Real, nodes[0][1].Real);
            for (int i = 1; i < nodes.Count; i++)
            {
                subpath.AppendLineSegment(nodes[i][0].Real, nodes[i][1].Real);
            }

            subpath.Closed = closed;

            Shape shape = Layer.CreateCurve(curve);
            SetStandardProperties(shape);

            if (name != null) shape.Name = name;

            return shape;
        }

        public Shape CreateLine(VectorC2 point1, VectorC2 point2, bool betweenborders = true, string name = null)
        {
            if (betweenborders)
            {
                var line = bordertoborder(point1, point2);
                if (line == null)
                {
                    return null;
                }

                point1 = line[0];
                point2 = line[1];
            }

            var curve = new Curve();
            var subpath = curve.CreateSubPath(point1[0].Real, point1[1].Real);
            subpath.AppendLineSegment(point2[0].Real, point2[1].Real);

            subpath.Closed = false;

            Shape shape = Layer.CreateCurve(curve);
            SetStandardProperties(shape);

            if (name != null) shape.Name = name;

            return shape;
        }

        VectorC2[] bordertoborder(VectorC2 point1, VectorC2 point2)
        {
            if (point1.Equals(point2)) { return null; }

            var rv = new VectorC2[2];

            Line2D border_T_LR = new Point2D(TopLeft).Join(new Point2D(TopRight));
            Line2D border_L_BT = new Point2D(TopLeft).Join(new Point2D(BottomLeft));
            Line2D border_R_BT = new Point2D(BottomRight).Join(new Point2D(TopRight));
            Line2D border_B_LR = new Point2D(BottomRight).Join(new Point2D(BottomLeft));

            Line2D todraw = new Point2D(point1).Join(new Point2D(point2));

            var meet_T_LR_2d = todraw.Meet(border_T_LR);
            var meet_L_BT_2d = todraw.Meet(border_L_BT);
            var meet_R_BT_2d = todraw.Meet(border_R_BT);
            var meet_B_LR_2d = todraw.Meet(border_B_LR);

            var meet_T_LR = meet_T_LR_2d == null ? null : meet_T_LR_2d.ToAffine();
            var meet_L_BT = meet_L_BT_2d == null ? null : meet_L_BT_2d.ToAffine();
            var meet_R_BT = meet_R_BT_2d == null ? null : meet_R_BT_2d.ToAffine();
            var meet_B_LR = meet_B_LR_2d == null ? null : meet_B_LR_2d.ToAffine();

            if (meet_T_LR != null && Extensions.Between(TopLeft[0].Real, TopRight[0].Real, meet_T_LR[0].Real))
            {
                if (rv[0] == null)
                {
                    rv[0] = meet_T_LR;
                }
                else if (rv[1] == null)
                {
                    rv[1] = meet_T_LR;
                }
            }
            if (meet_L_BT != null && Extensions.Between(TopLeft[1].Real, BottomLeft[1].Real, meet_L_BT[1].Real))
            {
                if (rv[0] == null)
                {
                    rv[0] = meet_L_BT;
                }
                else if (rv[1] == null)
                {
                    rv[1] = meet_L_BT;
                }
            }
            if (meet_B_LR != null && Extensions.Between(BottomLeft[0].Real, BottomRight[0].Real, meet_B_LR[0].Real))
            {
                if (rv[0] == null)
                {
                    rv[0] = meet_B_LR;
                }
                else if (rv[1] == null)
                {
                    rv[1] = meet_B_LR;
                }
            }
            if (meet_R_BT != null && Extensions.Between(TopRight[1].Real, BottomRight[1].Real, meet_R_BT[1].Real))
            {
                if (rv[0] == null)
                {
                    rv[0] = meet_R_BT;
                }
                else if (rv[1] == null)
                {
                    rv[1] = meet_R_BT;
                }
            }

            if (rv[0] != null && rv[1] != null)
            {
                return rv;
            }
            else
            {
                return null;
            }
        }

        public Shape CreateCurve(List<VectorC2> nodes, bool closed = false, string name = null)
        {
            if (nodes == null) throw new ArgumentNullException("nodes");
            if (nodes.Count < 2) throw new ArgumentException("minimum of 2 nodes required");

            var curve = new Curve();
            var subpath = curve.CreateSubPath(nodes[0][0].Real, nodes[0][1].Real);
            for (int i = 1; i < nodes.Count; i++)
            {
                subpath.AppendCurveSegment(nodes[i][0].Real, nodes[i][1].Real);
            }

            subpath.Closed = closed;

            curve.Segments.All().SetType(cdrSegmentType.cdrCurveSegment);
            curve.Nodes.All().SetType(cdrNodeType.cdrSmoothNode);

            Shape shape = Layer.CreateCurve(curve);
            SetStandardProperties(shape);

            if (name != null) shape.Name = name;

            return shape;
        }

        public Shape CreateCurve(List<List<VectorC2>> nodes)
        {
            var shaperange = new ShapeRange();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Count > 1)
                {
                    var shape = CreateCurve(nodes[i], closed: false);
                    shaperange.Add(shape);
                }
            }

            return shaperange.Combine();
        }
    }

    public class DrawAttribute : Attribute
    {
        public DrawAttribute(bool draw)
        {
            Draw = draw;
            PageOrientation = VGCore.cdrPageOrientation.cdrLandscape;
        }
        public bool Draw { get; private set; }

        public VGCore.cdrPageOrientation PageOrientation { get; set; }
    }
}
