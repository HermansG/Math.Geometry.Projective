using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

// Requirement: install CorelDRAW and if necessary change the references:
// CorelDRAW  -   CorelDRAW 17.1 Type Library             == CorelDRAW X7
// VGCore     -   Vector Graphics Core 17.1 Type Library  == CorelDRAW X7
// to the CorelDRAW version on your machine.

// This automation is meant to be used from Visual Studio (simply in debug mode).
// The combination MathNet.Numerics + Projective.Geometry + Visual Studio with C# + CorelDRAW combines the best of different worlds.

// Performance: the performance of the CorelDRAW Interop is rather slow. So this is not about performance, 
// but just about applying mathematical techniques to get the required drawing in CorelDRAW.

namespace CorelDraw.Automation
{
    class Program
    {
        static void Main(string[] args)
        {
            CorelDRAW.Application app = null;

            bool error = false;

            try
            {
                Console.WriteLine("\n\nStarting CorelDRAW - this may take a minute.");
                app = new CorelDRAW.Application();
                app.AppWindow.WindowState = VGCore.cdrWindowState.cdrWindowMaximized;

                app.Visible = true;
                // You may want to experiment whether setting the next properties to true/false improves performance
                app.Optimization = false;
                app.EventsEnabled = true;

                var document = app.CreateDocument();
                document.Name = "c#";
                document.ResetSettings();
                document.Unit = VGCore.cdrUnit.cdrMillimeter;
                // Given the x,y-positon of a Shape, ReferencePoint determines which TB-LR-C coordinates of the shape are actually used
                document.ReferencePoint = VGCore.cdrReferencePoint.cdrCenter;
                // Nota bene: we leave the origin X = 0, Y = 0 at the BottomLeft corner of the ActivePage.
                document.SaveSettings();
                document.ClearUndoList();

                var startpage = document.ActivePage;

                var assembly = Assembly.GetExecutingAssembly();
                var typestodraw = new List<Type>();
                foreach (Type type in assembly.GetTypes().Where(p => !p.IsAbstract && p.IsSubclassOf(typeof(DrawingBase))))
                {
                    var attribute = type.GetCustomAttribute<DrawAttribute>();
                    if (attribute != null && attribute.Draw)
                    {
                        typestodraw.Add(type);
                    }
                }

                for (int i = 0; i < typestodraw.Count; i++)
                {
                    // Draw on separate pages.
                    VGCore.Page page = i == 0 ? startpage : document.AddPages(1);

                    page.Name = typestodraw[i].Name;

                    var drawing = Activator.CreateInstance(typestodraw[i], document, page) as DrawingBase;
                    if (drawing != null)
                    {
                        Console.Write("\n\nStart drawing " + typestodraw[i].Name + " ...");
                        drawing.CreateDrawing();
                        Console.WriteLine(" ready.");
                    }
                }
                document.ClearSelection();
                startpage.Activate();
                app.ActiveWindow.ActiveView.ToFitAllObjects();
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine("\n\nAn eror occured:\n\n" + ex.GetType() + ": " + ex.Message);
                Console.WriteLine("\nStacktrace:\n\n" + ex.StackTrace);
            }
            finally
            {
                app.Visible = true;
                app.Optimization = false;
                app.EventsEnabled = true;

                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(app);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (error)
                {
                    Console.WriteLine("\n\nPress any key to exit");
                    Console.ReadKey();
                }
            }
        }
    }
}
