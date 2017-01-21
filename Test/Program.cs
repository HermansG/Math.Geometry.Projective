using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Test
{
    using System.Numerics;
    using MathNet.Numerics;
    using Geometry.Projective;
    using Ext = Geometry.Projective.Extensions;
    using MathNet.Numerics.LinearAlgebra.Complex;
    using MathNet.Numerics.LinearAlgebra;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var test = new HVector(Complex.ImaginaryOne, Complex.ImaginaryOne, Complex.ImaginaryOne);
                test.ToVector().CoerceHomogeneousCoordinates();

                for (int i = 0; i < 10000; i++)
                {
                    var centralcollineation1 = Collineation2D.CreateCentralCollineation(Point2D.Origin, Line2D.Infinity, 2);
                    Point2D BC = new Point2D(1, 1.6, 0.8);
                    Line2D b = new Line2D(-2, 1, 0);
                    Point2D P = new Point2D(1, 1, -1);
                    Point2D Q = new Point2D(1, 1, 0);
                    Point2D R = new Point2D(1, 1, 1);
                    Point2D Qacc = centralcollineation1.Map(Q);
                    Point2D Qaccacc = centralcollineation1.Map(Qacc);
                    var centralcollineation2 = Collineation2D.CreateCentralCollineation(BC, b, Qaccacc, R);

                    var matrix = centralcollineation2.Matrix.Multiply(centralcollineation1.Matrix);
                }

                Console.WriteLine("Test 1\n");

                var origin = new Point2D(1, 0, 0);
                var infinity = new Point2D(1, 3, 2.5);
                var unity = new Point2D(origin.ToVector() + infinity.ToVector());
                var fourth = new Point2D(2 * origin.ToVector() + 6 * infinity.ToVector());

                Complex crossratio = Ext.CrossRatio(new Set<Point2D>(origin, infinity, unity, fourth).ToHVector());

                Console.WriteLine("Cross ration = " + crossratio.ToString());

                Console.WriteLine("\nTest 2\n");

                double[] array = new double[] { 3, -2, 1 };
                var point = new Point2D(array);

                Console.WriteLine(point.ToString() + "\n10 different lines through this point:\n");

                var lines = new List<Line2D>();
                for (int i = 0; i < 10; i++)
                {
                    var newline = point.GetLine(true, lines);
                    lines.Add(newline);
                    Console.WriteLine(newline.ToString());
                }

                Console.WriteLine("\nTest 3\n");

                var line = new Line2D(3);

                Console.WriteLine(line.ToAffineString() + "\n10 different points on this line:\n");

                var points = new List<Point2D>();
                for (int i = 0; i < 10; i++)
                {
                    var newpoint = line.GetPoint(true, points);
                    points.Add(newpoint);
                    Console.WriteLine(newpoint.ToString());
                }

                Console.WriteLine("\nTest 4\n");

                Console.WriteLine(Line3D.Xaxis.ToString() + "\tx-axis");
                Console.WriteLine(Line3D.Yaxis.ToString() + "\ty-axis");
                Console.WriteLine(Line3D.Zaxis.ToString() + "\tz-axis");
                Console.WriteLine(Line3D.InfinityXY.ToString() + "\tinfinity-xy");
                Console.WriteLine(Line3D.InfinityXZ.ToString() + "\tinfinity xz");
                Console.WriteLine(Line3D.InfinityYZ.ToString() + "\tinfinity yz");
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.GetType().Name + ":\n" + exc.Message, true);
                if (exc.InnerException != null)
                {
                    Console.WriteLine(exc.InnerException.Message, true);
                    if (exc.InnerException.InnerException != null)
                    {
                        Console.WriteLine("\n" + exc.InnerException.InnerException.Message, true);
                    }
                }
                var trace = new System.Diagnostics.StackTrace(exc, true);
                var frames = trace.GetFrames();
                for (int i = frames.Count() - 1; i >= 0; i--)
                {
                    string message = new string(' ', 2 * (frames.Count() - 1 - i)) + "> ";
                    var frame = frames[i];
                    var filename = frame.GetFileName();
                    if (filename != null)
                    {
                        var path = filename.Split('\\');
                        message += path[path.Count() - 1];
                    }
                    message += " -- " + frame.GetMethod().Name;
                    message += " -- r. " + frame.GetFileLineNumber();
                    Console.WriteLine(message);
                }
            }
            finally
            {
                Console.WriteLine("\nPress a key to exit");
                Console.ReadKey();
            }
        }
    }
}
