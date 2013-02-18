using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using mshtml;
using System.IO;
using Microsoft.Surface.Core;
using System.Reflection;
using System.ComponentModel;
using System.Collections;


namespace WebBrowser
{
    /// <summary>
    /// Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : SurfaceWindow
    {
        private TouchTarget touchTarget;
        IntPtr hwnd;
        Queue<Point> coord_pack_Q = new Queue<Point>();
        double prev_x;
        double prev_y;
        bool found = false;
        Hashtable dom_cord;
        Hashtable cord_dom;

        /// Default constructor.
        public SurfaceWindow1()
        {
            InitializeComponent();
            // Add handlers for window availability events
            AddWindowAvailabilityHandlers();
            InitializeSurfaceInput();
        }

        private void InitializeSurfaceInput()
        {
            if (touchTarget != null)
                return;
            // Get the hwnd for the SurfaceWindow object after it has been loaded.
            hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            touchTarget = new Microsoft.Surface.Core.TouchTarget(hwnd);
            // Set up the TouchTarget object for the entire SurfaceWindow object.
            touchTarget.EnableInput();    
        }

        private void OnTouchTargetFrameReceived(object sender, FrameReceivedEventArgs e)
        {
            ReadOnlyTouchPointCollection touches = touchTarget.GetState();
            prev_x = 0.0;
            prev_y = 0.0;

            getcords(touches);
        }

        private void getcords(ReadOnlyTouchPointCollection touches)
        {
            // for each touch points, get the links near that point.
            // Add them into the hash table with element as key, coordinate as value.
            foreach (TouchPoint touch in touches){
                if (((double)touch.CenterX != prev_x) && ((double)touch.CenterY != prev_y))
                {
                    prev_x = (double)touch.CenterX;
                    prev_y = (double)touch.CenterY;
                    Console.WriteLine(prev_x + "," + prev_y);
                    
                    Point coord = new Point((double)touch.CenterX - 209, (double)touch.CenterY - 128);
                    coord_pack_Q.Enqueue(coord);

                    if (coord_pack_Q.Count() == 50) // track only 50 points for the history.
                    {
                        Console.WriteLine("cluster limit reached");
                        coord_pack_Q.Dequeue(); // pull the oldest element
                    }

                    if (touch.IsFingerRecognized) //clicked?
                    {
                        Console.WriteLine("Finger detected");
                        sendCoord(touch);
                        webBrowser1.Navigating -= webBrowser1_Navigating;
                        if (dom_cord.Count > 0)
                        {
                            found = true;
                        }
                        proceed_estimation();
                    }
                }
            }
        }

        private void proceed_estimation()
        {   
            // compare with the estimated point coordinate or
            // very previous point
            // =>>> To the nearest element.
            PointCollection est_point = approximater();
            Point prev_touched = new Point(prev_x, prev_y);
            Console.WriteLine("Start estimation");
            String togo = surfaceTextBox1.Text;  //webBrowser1.Source.AbsolutePath;

            // Get the nearest href link
            // From where we have collected previously.
            Point tup;
            String href;
            double distance = 0;
            Hashtable d_list = new Hashtable();
            foreach (DictionaryEntry de in dom_cord)
            {
                tup = de.Value as Point;
                href = de.Key as String;
                // get distance from each element collected, to the last touched point.
                distance = Point.FindDistance(tup, prev_touched);
                // add distances with coordinates.
                d_list.Add(distance, tup);
            }
            Console.WriteLine("Find the nearest DOM");
            // Find the shortest distance from the point.
            double lowest = 30000.0;
            foreach (double d in d_list.Keys)
            {
                if (d < lowest)
                {
                    lowest = d;
                }
            }
            Console.WriteLine(lowest.ToString());

            // coordinate of the shortest distance: coordinate = d_list[lowest]
            // dom object from the coordinate: togo = cord_dom[coordinate]
            
            // Navigate to the decided link.
            if (found)
            {
                Point np = (Point)d_list[lowest];
                Console.WriteLine(np.X + "," + np.Y);
                togo = (String)cord_dom[np];
                Console.WriteLine(togo);
            
                try
                {
                    webBrowser1.Navigate(togo);
                }
                catch { }
                found = false;
                Console.WriteLine("done");
                Console.WriteLine(DateTime.Now);
                touchTarget.FrameReceived -= OnTouchTargetFrameReceived;
            }
        }

        /* takes recent n points, 
         * proceeds to get approximated coordinate where the finger was heading to.*/
        private PointCollection approximater()
        {
            // Find objects near these estimated points.
            IHTMLDocument2 webdoc = (IHTMLDocument2)webBrowser1.Document;
            double run = 0.0;
            double prev_x = 0.0;
            double avg_x = 0.0;
            double rise = 0.0;
            double prev_y = 0.0;
            double avg_y = 0.0;
            
            PointCollection recent_points = new PointCollection();
            Console.WriteLine("Gather recent points");

            // add the points into pointcollection
            foreach (Point p in coord_pack_Q.ToList())
            {
                recent_points.AddPoint(p);
            }
            Point centroid1 = new Point(0.0, 0.0);
            Point centroid2 = new Point(0.0, 0.0);
            if (coord_pack_Q.Count > 40)
            {
                Console.WriteLine("Do Clustering");
                // Get clusters.
                List<PointCollection> allclusters = Clustering.doClustering(recent_points, 2);
                centroid1 = allclusters[0].Centroid;
                centroid2 = allclusters[1].Centroid;
                get_around(webdoc, (int)centroid1.X, (int)centroid1.Y);
                Console.WriteLine("get around centroid1 at:" + centroid1.X.ToString() + "," + centroid1.Y.ToString());
                get_around(webdoc, (int)centroid2.X, (int)centroid2.Y);
                Console.WriteLine("get around centroid2 at:" + centroid2.X.ToString() + "," + centroid2.Y.ToString());
            }
            
            //Console.WriteLine("find linear estimation");
            // Find average growth rate from list.
            // This is for linear movement.
            /*
            foreach (Point cord in coord_pack_Q.ToList())
            {
                run = cord.X - prev_x;
                prev_x = cord.X;
                avg_x = (avg_x + run);
                rise = cord.Y - prev_y;
                prev_y = cord.Y;
                avg_y = (avg_y + rise);
            }
            avg_x = avg_x / coord_pack_Q.Count();
            avg_y = avg_y / coord_pack_Q.Count();
            Point estimate = new Point(avg_x, avg_y);

            get_around(webdoc, (int)(estimate.X), (int)(estimate.Y));
            Console.WriteLine("get around linear estimation point at:" + estimate.X.ToString() + "," + estimate.Y.ToString());
            */

            PointCollection near_points = new PointCollection();
            //near_points.AddPoint(estimate);
            near_points.AddPoint(centroid1);
            near_points.AddPoint(centroid2);

            return near_points;
        }

        private void sendCoord(TouchPoint toch)
        {
            double xcord = (double)(toch.CenterX) - 209;
            double ycord = (double)(toch.CenterY) - 128;

            Console.WriteLine(xcord + "," + ycord);

            IHTMLDocument2 webdoc = (IHTMLDocument2)webBrowser1.Document;
            get_around(webdoc, (int)xcord, (int)ycord);
        }
        /*
         *  Takes webdoc from control.webbrowser. also two int for x-y coordinates.
         *  It takes a point, and find any DOM objects around the point. (with bound of +- 10)
         *  Ultimately, it adds to hashtable dom_cord with
         *  the DOM element (href) as key
         *  position coordinate Point was value (cord_dom -> key: cord value: dom)
         * 
         */
        private void get_around(IHTMLDocument2 webdoc, int xcord, int ycord)
        {
            for (int i = 0; i < 30; i+=3)
            {
                for (int j = 0; j < 30; j+=3)
                {
                    try
                    {
                        var obj_1 = webdoc.elementFromPoint(xcord + i, ycord + j).getAttribute("href");
                        var obj_2 = webdoc.elementFromPoint(xcord - i, ycord + j).getAttribute("href");
                        var obj_3 = webdoc.elementFromPoint(xcord + i, ycord - j).getAttribute("href");
                        var obj_4 = webdoc.elementFromPoint(xcord - i, ycord - j).getAttribute("href");

                        if (obj_1 is String)
                        {   if (obj_1.Length > 0)
                            {
                                Point ele_coord = new Point(xcord + i, ycord + j);
                                dom_cord.Add(obj_1, ele_coord);
                                cord_dom.Add(ele_coord, obj_1);
                            }
                        }
                        if (obj_2 is String)
                        {   if (obj_2.Length > 0)
                            {
                                Point ele_coord = new Point(xcord - i, ycord + j);
                                dom_cord.Add(obj_2, ele_coord);
                                cord_dom.Add(ele_coord, obj_2);
                            }
                        }
                        if (obj_3 is String)
                        {   if (obj_3.Length > 0)
                            {
                                Point ele_coord = new Point(xcord + i, ycord - j);
                                dom_cord.Add(obj_3, ele_coord);
                                cord_dom.Add(ele_coord, obj_3);
                            }
                        }
                        if (obj_4 is String)
                        {
                            if (obj_4.Length > 0)
                            {
                                Point ele_coord = new Point(xcord - i, ycord - j);
                                dom_cord.Add(obj_4, ele_coord);
                                cord_dom.Add(ele_coord, obj_4);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private void webBrowser1_LoadCompleted(object sender, NavigationEventArgs e)
        {
            //DateTime now = DateTime.Now;
            Console.WriteLine("Web page loaded");
            Console.WriteLine(DateTime.Now);

            // Attach an event handler for the FrameReceived 
            touchTarget.FrameReceived += OnTouchTargetFrameReceived;

            try
            {
                IHTMLDocument2 webdoc = (IHTMLDocument2)webBrowser1.Document;
                HTMLDocumentEvents2_Event iEvent;
                iEvent = (HTMLDocumentEvents2_Event)webdoc;
                iEvent.onclick += new mshtml.HTMLDocumentEvents2_onclickEventHandler(clicked);
                //Prevent unwanted actions to be done.
                webBrowser1.Navigating += new NavigatingCancelEventHandler(webBrowser1_Navigating);
                dom_cord = new Hashtable();
                cord_dom = new Hashtable();
                coord_pack_Q = new Queue<Point>();
            }
            catch (Exception){}
            //string var = File.ReadAllText("C:/Users/surfaceadmin/Dropbox/494/WebBrowser/WebBrowser/Resources/script.txt");
            //webdoc.parentWindow.execScript(var, "JScript");
        }

        void webBrowser1_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private bool clicked(mshtml.IHTMLEventObj e) {
            if (e.srcElement.tagName == "A")
            {
                e.cancelBubble = true;
            }
            return false;
        }

        /// <summary>
        /// Occurs when the window is about to close. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Remove handlers for window availability events
            RemoveWindowAvailabilityHandlers();
        }
        /// <summary>
        /// Adds handlers for window availability events.
        /// </summary>
        private void AddWindowAvailabilityHandlers()
        {
            // Subscribe to surface window availability events
            ApplicationServices.WindowInteractive += OnWindowInteractive;
            ApplicationServices.WindowNoninteractive += OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable += OnWindowUnavailable;
        }

        /// <summary>
        /// Removes handlers for window availability events.
        /// </summary>
        private void RemoveWindowAvailabilityHandlers()
        {
            // Unsubscribe from surface window availability events
            ApplicationServices.WindowInteractive -= OnWindowInteractive;
            ApplicationServices.WindowNoninteractive -= OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable -= OnWindowUnavailable;
        }

        /// <summary>
        /// This is called when the user can interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowInteractive(object sender, EventArgs e)
        {
            // Turn raw image back on again
            //touchTarget.EnableImage(ImageType.Normalized);
        }

        /// <summary>
        /// This is called when the user can see but not interact with the application's window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowNoninteractive(object sender, EventArgs e)
        {
            //TODO: Disable audio here if it is enabled

            //TODO: optionally enable animations here
        }

        /// <summary>
        /// This is called when the application's window is not visible or interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            // If the app isn't active, there's no need to keep the raw image enabled
            //touchTarget.DisableImage(ImageType.Normalized);
        }

        private void surfaceButton1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                webBrowser1.GoBack();
            }
            catch (Exception)
            {
                Console.WriteLine("back exception");
            }
        }

        private void surfaceButton2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                webBrowser1.GoForward();
            }
            catch { }
        }

        private void surfaceButton3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                webBrowser1.Navigate(surfaceTextBox1.Text);
                //webBrowser1.Navigate("http://news.ycombinator.com");
            }
            catch (UriFormatException)
            {
                try
                {
                    webBrowser1.Navigate("http://" + surfaceTextBox1.Text);
                }
                catch { }
            }
        }

        private void surfaceButton4_Click(object sender, RoutedEventArgs e)
        {
            webBrowser1.Refresh();
        }
    }
}