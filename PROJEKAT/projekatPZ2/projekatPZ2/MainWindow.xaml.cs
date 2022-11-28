using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using projekatPZ2.Entiteti;

namespace projekatPZ2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MapData mapData = new MapData(500, 500);  
        //koordinate mape:
        private static double lat1 = 45.2325;
        private static double lon1 = 19.793909;
        private static double lat2 = 45.277031;
        private static double lon2 = 19.894459;
        //velicine entiteta:
        private static double velicinaKocke = 0.006;
        private static double velicinaLinije = 0.002;
        //strukture:
        private Dictionary<Point, int> zauzetePozicije = new Dictionary<Point, int>();
        private Dictionary<long, GeometryModel3D> cvorovi = new Dictionary<long, GeometryModel3D>();
        private List<GeometryModel3D> vodovi = new List<GeometryModel3D>();
        //events
        public bool ucitano = false;
        private Point start = new Point();
        private Point pomerajMapePoOsi = new Point();
        private static int zoomCurent = 1;
        private static int zoomMax = 25;
        private static int zoomMin = 5;
        private bool srednjiTockic = false;
        private Point pozicijaSrednjegTockica;
        private static double ugaoRotacije = 0.5;
        private GeometryModel3D hitTest;
        //dodatne strukture:
        private Dictionary<long, GeometryModel3D> entitetiZaToolTip = new Dictionary<long, GeometryModel3D>();
        private List<GeometryModel3D> linijeZaToolTip = new List<GeometryModel3D>();
        //tooltip helpers:
        public static readonly DependencyProperty toolTipSign = 
                            DependencyProperty.RegisterAttached("Tag", typeof(string), typeof(GeometryModel3D));
        private ToolTip tt = new ToolTip();
        public static readonly DependencyProperty toolTipStartPoint = DependencyProperty.RegisterAttached("Start", typeof(long), typeof(GeometryModel3D));
        public static readonly DependencyProperty toolTipEndPoint = DependencyProperty.RegisterAttached("End", typeof(long), typeof(GeometryModel3D));
        //dodatni interfejs:
        Dictionary<long, GeometryModel3D> otvoreni = new Dictionary<long, GeometryModel3D>();
        List<GeometryModel3D> zatvoreni = new List<GeometryModel3D>();

        List<GeometryModel3D> crveniVodovi = new List<GeometryModel3D>();
        List<GeometryModel3D> narandzastiVodovi = new List<GeometryModel3D>();
        List<GeometryModel3D> zutiVodovi = new List<GeometryModel3D>();

        List<GeometryModel3D> neaktivniDeoMreze = new List<GeometryModel3D>();
        List<GeometryModel3D> secondEnd = new List<GeometryModel3D>();

        List<GeometryModel3D> option1 = new List<GeometryModel3D>();    //0-1
        List<GeometryModel3D> option2 = new List<GeometryModel3D>();    //1-2
        List<GeometryModel3D> option3 = new List<GeometryModel3D>();    //2+

      

        public MainWindow()
        {
            InitializeComponent();
          //Model_Click(null,null);
        }

        #region CRTANJE
        public Dictionary<long,GeometryModel3D> CrtajCvorove(Dictionary<long,PowerEntity> entiteti)
        {
            //provera da li se entitet nalazi u okviru mape(ako je van ignore)
            foreach(var e in entiteti.Values)
            {
                if (e.X < 45.2325 || e.X > 45.277031 
                    || e.Y > 19.894459  || e.Y < 19.793909)
                {
                    //ignore entity
                }
                else
                {
                    e.X = Math.Round(e.X, 3);
                    e.Y = Math.Round(e.Y, 3);

                    e.novoX = (e.X - lat1) / (lat2 - lat1) * (1 - velicinaKocke)-0.5;       //CHECK
                    e.novoY = (e.Y - lon1) / (lon2 - lon1) * (1 - velicinaKocke)-0.5;

                    Point pt = new Point(e.novoY, e.novoX);
                    if (zauzetePozicije.ContainsKey(pt))
                    {
                        //stavimo ga iznad vec postojaceg na toj poziciji
                        zauzetePozicije[pt]++;
                    }
                    else
                    {
                        //0=value=koji je po redu na lokaciji pt
                        zauzetePozicije.Add(pt, 0);
                    }

                    //crtanje 3D entiteta:
                    GeometryModel3D model = new GeometryModel3D();
                    if (e.GetType().Equals(typeof(SubstationEntity)))
                    {
                        model.Material = new DiffuseMaterial(Brushes.Blue);
                    }
                    else if (e.GetType().Equals(typeof(NodeEntity)))
                    {
                        model.Material = new DiffuseMaterial(Brushes.Blue);
                    }
                    else if (e.GetType().Equals(typeof(SwitchEntity)))
                    {
                        if (((SwitchEntity)e).Status == "Open")
                        {
                            model.Material = new DiffuseMaterial(Brushes.Blue);
                            otvoreni.Add(((SwitchEntity)e).Id, model);
                        }
                        else
                        {
                            model.Material = new DiffuseMaterial(Brushes.Blue);
                            zatvoreni.Add(model);
                        }
                    }
                    model.SetValue(toolTipSign, $"ID:{e.Id} NAME:{e.Name}");

                    //pomeraj po Z osi da bi se nacrtali jedan preko drugi
                    int z = 0;  
                    if (zauzetePozicije.ContainsKey(pt))
                    {
                        z = zauzetePozicije[pt];
                    }

                    // 8 tacaka = temena kocke
                    var points = new Point3DCollection()
                    { 
                        new Point3D(pt.X - velicinaKocke/2 , pt.Y + velicinaKocke/2 , z * velicinaKocke),
                        new Point3D(pt.X - velicinaKocke/2 , pt.Y - velicinaKocke/2 , z * velicinaKocke),
                        new Point3D(pt.X + velicinaKocke/2 , pt.Y - velicinaKocke/2 , z * velicinaKocke),
                        new Point3D(pt.X + velicinaKocke/2 , pt.Y + velicinaKocke/2 , z * velicinaKocke),
                        // prvo i peto teme imaju isti x,y ali su jedno iznad drugog pa mora da se digne po z osi
                        new Point3D(pt.X - velicinaKocke/2 , pt.Y + velicinaKocke/2 , velicinaKocke + z * velicinaKocke),
                        new Point3D(pt.X - velicinaKocke/2 , pt.Y - velicinaKocke/2 , velicinaKocke + z * velicinaKocke),
                        new Point3D(pt.X + velicinaKocke/2 , pt.Y - velicinaKocke/2 , velicinaKocke + z * velicinaKocke),
                        new Point3D(pt.X + velicinaKocke/2 , pt.Y + velicinaKocke/2 , velicinaKocke + z * velicinaKocke),
                    };

                    //entitei se iscrtavaju kao kocke-> 12 trouglova(po dva za svaku stranicu kocke)
                    var indicies = new Int32Collection()
                    {
                        2,3,1, 1,3,0, 5,6,7, 7,4,5, 1,2,6, 1,6,5, 0,1,5, 0,5,4, 2,3,7, 2,7,6, 0,3,7, 0,7,4
                    };

                    model.Geometry = new MeshGeometry3D() { Positions = points, TriangleIndices = indicies };
                    MAPA.Children.Add(model);
                    cvorovi.Add(e.Id,model);
                }             

            }
            return cvorovi;                                
        }
        public List<GeometryModel3D> CrtajVodove(Dictionary<long,LineEntity> lines)
        {
            foreach(LineEntity l in lines.Values)
            {
                List<Point> listaTacaka = new List<Point>();
                foreach(Point pt in l.TackeLinije)
                {
                    double novoX, novoY;
                    if (pt.X < 45.2325 || pt.X > 45.277031 || pt.Y > 19.894459 || pt.Y < 19.793909)
                    {
                        //ignore entity ako je van
                    }
                    else
                    {
                        novoX = Math.Round(pt.X, 3);
                        novoY = Math.Round(pt.Y, 3);

                        novoX = (novoX - lat1) / (lat2 - lat1) * (1 - velicinaLinije) - 0.5;      
                        novoY = (novoY - lon1) / (lon2 - lon1) * (1 - velicinaLinije) - 0.5;

                        //ukoliko sve tacke linije zadovoljavaju uslov(nisu van) onda ih dodajemo u listu tacaka te linije
                        //u suprotnom se lista ne pravi i ignorisemo celu liniju makar joj samo jedna tacka bila van               
                        listaTacaka.Add(new Point(novoY,novoX)); 
                    }
                }

                for(int i=1; i<listaTacaka.Count; i++)
                {
                    GeometryModel3D modelLinije = new GeometryModel3D();
                   // modelLinije.Material = new DiffuseMaterial(Brushes.Black);
                   

                    switch(l.MaterijalVoda)
                    {
                        case MaterijalVoda.Copper:
                            modelLinije.Material = new DiffuseMaterial(Brushes.Brown);
                            break;
                        case MaterijalVoda.Steel:
                            modelLinije.Material = new DiffuseMaterial(Brushes.Gray);
                            break;
                        case MaterijalVoda.Acsr:
                            modelLinije.Material = new DiffuseMaterial(Brushes.DarkGreen);
                            break;
                        case MaterijalVoda.Other:
                            modelLinije.Material = new DiffuseMaterial(Brushes.Black);
                            break;

                    }
                    modelLinije.SetValue(toolTipStartPoint, l.FirstEnd);
                    modelLinije.SetValue(toolTipEndPoint, l.SecondEnd);
                  

                    var points = new Point3DCollection()
                    {
                        //tacke mash modela 
                        //za svaku tacku navodimo x,y,z zbog 3D iscrtavanja
                        new Point3D(listaTacaka[i].X - velicinaLinije/2 , listaTacaka[i].Y + velicinaLinije/2 , velicinaLinije),  
                        new Point3D(listaTacaka[i].X - velicinaLinije/2 , listaTacaka[i].Y - velicinaLinije/2 , velicinaLinije),
                        new Point3D(listaTacaka[i-1].X+ velicinaLinije/2 , listaTacaka[i-1].Y - velicinaLinije/2 , velicinaLinije),
                        new Point3D(listaTacaka[i-1].X+ velicinaLinije/2 , listaTacaka[i-1].Y + velicinaLinije/2 , velicinaLinije),

                        new Point3D(listaTacaka[i].X - velicinaLinije/2 , listaTacaka[i].Y + velicinaLinije/2 , velicinaLinije),
                        new Point3D(listaTacaka[i].X - velicinaLinije/2 , listaTacaka[i].Y - velicinaLinije/2 , velicinaLinije),
                        new Point3D(listaTacaka[i-1].X+ velicinaLinije/2 , listaTacaka[i-1].Y - velicinaLinije/2 , velicinaLinije),
                        new Point3D(listaTacaka[i-1].X+ velicinaLinije/2 , listaTacaka[i-1].Y + velicinaLinije/2 , velicinaLinije),
                    };

                    //trouglovi iz kojih ce se sastojati model 
                    var indicies = new Int32Collection()
                    {
                         2,3,1, 1,3,0, 5,6,7, 7,4,5, 1,2,6, 1,6,5, 0,1,5, 0,5,4, 2,3,7, 2,7,6, 0,3,7, 0,7,4

                    };

                    modelLinije.Geometry = new MeshGeometry3D() { Positions = points, TriangleIndices = indicies };
                    MAPA.Children.Add(modelLinije);
                    vodovi.Add(modelLinije);
                    
                    //dodatni treci
                    if(l.Otpornost < 1)
                    {
                        crveniVodovi.Add(modelLinije);
                    }
                    else if(l.Otpornost >=1 && l.Otpornost <= 2)
                    {
                        narandzastiVodovi.Add(modelLinije);
                    }
                    else if(l.Otpornost > 2)
                    {
                        zutiVodovi.Add(modelLinije);
                    }

                    //moj dodatni(4)
                    if(l.Otpornost >= 0 && l.Otpornost <=1 )
                    {
                        option1.Add(modelLinije);
                    }
                    else if(l.Otpornost >=1 && l.Otpornost <=2)
                    {
                        option2.Add(modelLinije);
                    }
                    else if(l.Otpornost >2)
                    {
                        option3.Add(modelLinije);
                    }
                    else
                    {
                        //
                    }


                    //dodatni prvi
                    GeometryModel3D first = (GeometryModel3D)MAPA.Children.FirstOrDefault(node =>
                    (node.GetValue(FrameworkElement.TagProperty) as PowerEntity)?.Id == l.FirstEnd);
                    GeometryModel3D second = (GeometryModel3D)MAPA.Children.FirstOrDefault(node =>
                   (node.GetValue(FrameworkElement.TagProperty) as PowerEntity)?.Id == l.SecondEnd);

                   if(first == null)
                    {
                        continue;
                    }
                   if(first.GetValue(FrameworkElement.TagProperty) is SwitchEntity)
                    {
                        SwitchEntity s = (SwitchEntity)first.GetValue(FrameworkElement.TagProperty);
                        if(s.Status == "Open")
                        {
                            neaktivniDeoMreze.Add(modelLinije);
                            secondEnd.Add(second);
                        }
                    }

                }                
            }
            return vodovi;
        }
        #endregion

        #region UCITAVANJE
        private void Model_Click(object sender, RoutedEventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");

            XmlNodeList nodeList;
            double x, y;

            if (!ucitano)
            {
                #region SUBSTITUTIONS
                nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
                // parsiranje substitutuion entiteta         
                SubstationEntity substationEntity;
                foreach (XmlNode node in nodeList)
                {
                    substationEntity = new SubstationEntity
                    {
                        Id = long.Parse(node.SelectSingleNode("Id").InnerText),
                        Name = node.SelectSingleNode("Name").InnerText
                    };
                    x = double.Parse(node.SelectSingleNode("X").InnerText) + 40;
                    y = double.Parse(node.SelectSingleNode("Y").InnerText) + 80;
                    ToLatLon(x, y, 34, out double newX, out double newY);
                    substationEntity.X = newX;
                    substationEntity.Y = newY;
                    mapData.KolekcijaEntiteta.Add(substationEntity.Id, substationEntity);
                    // this.Close();
                }
                #endregion

                #region NODES
                nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
                NodeEntity nodeEntity;
                foreach (XmlNode node in nodeList)
                {
                    nodeEntity = new NodeEntity
                    {
                        Id = long.Parse(node.SelectSingleNode("Id").InnerText),
                        Name = node.SelectSingleNode("Name").InnerText
                    };
                    x = double.Parse(node.SelectSingleNode("X").InnerText) + 40;
                    y = double.Parse(node.SelectSingleNode("Y").InnerText) + 80;
                    ToLatLon(x, y, 34, out double newX, out double newY);
                    nodeEntity.X = newX;
                    nodeEntity.Y = newY;
                    mapData.KolekcijaEntiteta.Add(nodeEntity.Id, nodeEntity);
                    //  this.Close();
                }
                #endregion

                #region SWITCHES
                nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
                SwitchEntity switchEntity;
                foreach (XmlNode node in nodeList)
                {
                    switchEntity = new SwitchEntity
                    {
                        Id = long.Parse(node.SelectSingleNode("Id").InnerText),
                        Name = node.SelectSingleNode("Name").InnerText,
                        Status = node.SelectSingleNode("Status").InnerText
                    };
                    x = double.Parse(node.SelectSingleNode("X").InnerText) + 40;
                    y = double.Parse(node.SelectSingleNode("Y").InnerText) + 80;
                    ToLatLon(x, y, 34, out double newX, out double newY);
                    switchEntity.X = newX;
                    switchEntity.Y = newY;

                    mapData.KolekcijaEntiteta.Add(switchEntity.Id, switchEntity);
                    //this.Close();
                }
                #endregion

                #region LINES
                nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
                LineEntity line;
                foreach (XmlNode node in nodeList)
                {
                    line = new LineEntity
                    {
                        Id = long.Parse(node.SelectSingleNode("Id").InnerText),
                        Name = node.SelectSingleNode("Name").InnerText,
                        FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText),
                        SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText),
                        Otpornost = double.Parse(node.SelectSingleNode("R").InnerText)
                     
                    };
                    try
                    {
                        line.MaterijalVoda = (MaterijalVoda)Enum.Parse(typeof(MaterijalVoda), node.SelectSingleNode("ConductorMaterial").InnerText);

                    }
                    catch
                    {
                        line.MaterijalVoda = MaterijalVoda.Other;

                    }
                    

                    foreach (XmlNode pnode in node.ChildNodes[9].ChildNodes)
                    {
                        Point pt = new Point();
                        pt.X = double.Parse(pnode.SelectSingleNode("X").InnerText);
                        pt.Y = double.Parse(pnode.SelectSingleNode("Y").InnerText);

                        ToLatLon(pt.X, pt.Y, 34, out double newX, out double newY);
                        pt.X = newX;
                        pt.Y = newY;
                        line.TackeLinije.Add(pt);
                    }
                    mapData.KolekcijaLinija.Add(line.Id, line);

                }
                #endregion
                // this.Close();
              //  CrtajCvorove(mapData.KolekcijaEntiteta);
                foreach (var pair in CrtajCvorove(mapData.KolekcijaEntiteta))
                {
                    entitetiZaToolTip.Add(pair.Key, pair.Value);
                }
                //CrtajVodove(mapData.KolekcijaLinija);
                foreach (GeometryModel3D l in CrtajVodove(mapData.KolekcijaLinija))
                {
                    linijeZaToolTip.Add(l);
                }
                ucitano = true;

            }
        }
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
        #endregion

        #region EVENTS
        //panovati:
        private void event_LeviKlikut(object sender, MouseButtonEventArgs e)
        {
            // drzimo levi taster misa na element
            display.CaptureMouse(); 
            start = e.GetPosition(this);
            pomerajMapePoOsi.X = translacija.OffsetX;
            pomerajMapePoOsi.Y = translacija.OffsetY;
          
        }
        // Pomeraj 0 -- pomera se, a nista nije kliknuto
        // Pomeraj 1 -- pomera se, kliknut levi
        // Pomeraj 2 -- pomera se, kliknut tockic       
        private void event_Pomeraj(object sender, MouseEventArgs e)
        {
            if (display.IsMouseCaptured && srednjiTockic == false)
            {
                Point end = e.GetPosition(this);
                double offsetX = end.X - start.X;
                double offsetY = end.Y - start.Y;
                double w = this.Width;
                double h = this.Height;
                double translateX = -(offsetX * 100) / w;
                double translateY = +(offsetY * 100) / h;
                translacija.OffsetX = pomerajMapePoOsi.X + (translateX / (100 * skaliranje.ScaleX));
                translacija.OffsetY = pomerajMapePoOsi.Y + (translateY / (100 * skaliranje.ScaleX));
            }
            if(srednjiTockic==true)
            {
                display.CaptureMouse();
                Point mouse = e.GetPosition(this);
                //mouseX trenutna pozicija kursora , pozcijasrtockica-gde sam kliknula prvo sa srednjim tockicem
                double diffX = mouse.X - pozicijaSrednjegTockica.X;
                double diffY = mouse.Y - pozicijaSrednjegTockica.Y;

                //vektor pomeraja ide u suprotnom smeru od pomeraja misa (zato -1)
                //to nam je vektor izmedju fix tacke i trenutne pozicije misa
                //i on gleda ka fix tacki i potreban je da bismo oko njega radili rotaciju
                diffX *= -1;
                diffY *= -1; 
                
                rotacija.Axis = new Vector3D(diffY, diffX, 0); //osa rotacije (pomeraj po z je 0)
                rotacija.Angle = Math.Sqrt(diffX * diffX + diffY * diffY) * ugaoRotacije;// aptokdimacija kosinusne tereme

            }
        }
        private void event_LeviPusten(object sender, MouseButtonEventArgs e)
        {
            //oslobadja hvatanje misa ako je element drzao hvatanje
            display.ReleaseMouseCapture();
        }
            
        // za rotaciju - srednji
        private void event_NekiKlikut(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                srednjiTockic = true;
                pozicijaSrednjegTockica = e.GetPosition(this);
                return;
            }

            
        }
        private void event_NekiPusten(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                srednjiTockic = false;
                display.ReleaseMouseCapture();
            }
            tt.IsOpen = false;
            //ako smo kliknuli na levi i pusti ga->hocemo tooltip
            if(e.ChangedButton != MouseButton.Left)
            {
                //ignorisi sve druge klikove
            }
            else
            {
                Point mouseposition = e.GetPosition(this);
                Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0); //trenutni polozaj pokazivaca misa
                Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10); //vektor pravca u kom pokazivac pokazuje

                PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);  //gde se kliknulo na prozoru
                RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

                hitTest = null;

                //za pronalazenje kliknutog elementa(posto se iscrtani objekti smestaju u internu strukturu stabla)
                VisualTreeHelper.HitTest(display, null, HTResult, pointparams);
            }
        }
        
        //zoom
        private void Zoom_iraj(object sender, MouseWheelEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(this);
            double scaleX = 1;
            double scaleY = 1;
            if (e.Delta > 0 && zoomCurent < zoomMax)
            {
                scaleX = skaliranje.ScaleX + 0.1;
                scaleY = skaliranje.ScaleY + 0.1;
                zoomCurent++;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
            }
            else if (e.Delta <= 0 && zoomCurent > -zoomMin)
            {
                scaleX = skaliranje.ScaleX - 0.1;
                scaleY = skaliranje.ScaleY - 0.1;
                zoomCurent--;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
            }
        }
        #endregion

        private HitTestResultBehavior HTResult(HitTestResult rez)
        {
            RayHitTestResult rayrez = rez as RayHitTestResult;
            if (rayrez != null)
            {
                bool pomocna = false;
                foreach (var model in cvorovi.Values)             //za entitete
                {
                    if(pomocna == true)
                    {
                        break;
                    }
                    if (model == rayrez.ModelHit)                    //da li je kliknuto na entitet
                    {
                        hitTest = (GeometryModel3D)rayrez.ModelHit;
                        pomocna = true;
                       // za tooltip
                        tt.Content = (string)model.GetValue(toolTipSign); 
                        tt.IsOpen = true;
                    }
                }
                if (pomocna == false)
                {
                    foreach (var model in vodovi)
                    {
                        if (pomocna == true)
                        {
                            break;
                        }
                        if (model == rayrez.ModelHit)
                        {
                            hitTest = (GeometryModel3D)rayrez.ModelHit;
                            pomocna = true;
                            //tacke koje linija spaja
                            long sp = (long)model.GetValue(toolTipStartPoint); //id SP
                            long ep = (long)model.GetValue(toolTipEndPoint);   //id EP

                           entitetiZaToolTip[sp].Material = new DiffuseMaterial(Brushes.Purple);
                           entitetiZaToolTip[ep].Material = new DiffuseMaterial(Brushes.Purple);

                        }
                    }
                }
             
            }
            return HitTestResultBehavior.Stop; //zaustavimo hit test
        }

        #region dodatni interfejs i zadaci
        private void CheckSwitchStatus(object sender, RoutedEventArgs e)
        {
            foreach(GeometryModel3D m in otvoreni.Values)
            {                

                m.Material = new DiffuseMaterial(Brushes.Green);
               
            }
            foreach (GeometryModel3D m in zatvoreni)
            {
                m.Material = new DiffuseMaterial(Brushes.Red);

            }
        }

        private void UncheckSwitchStatus(object sender, RoutedEventArgs e)
        {

            foreach (GeometryModel3D m in otvoreni.Values)
            {

                m.Material = new DiffuseMaterial(Brushes.Blue);

            }
            foreach (GeometryModel3D m in zatvoreni)
            {
                m.Material = new DiffuseMaterial(Brushes.Blue);

            }
        }

        private void PromeniBojuVodovaNaOsnovuOtpornosti(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in crveniVodovi)
            {
                m.Material = new DiffuseMaterial(Brushes.Red);
            }
            foreach (GeometryModel3D m in narandzastiVodovi)
            {
                m.Material = new DiffuseMaterial(Brushes.Orange);
            }
            foreach (GeometryModel3D m in zutiVodovi)
            {
                m.Material = new DiffuseMaterial(Brushes.Yellow);
            }

        }

        private void VratiBojuVodovaNaOsnovuOtpornosti(object sender, RoutedEventArgs e)
        {
            //crveniVodovi = pamtiZaCrvene;


            //foreach (GeometryModel3D m in narandzastiVodovi)
            //{
            //    m.Material = m.BackMaterial;
            //}
            //foreach (GeometryModel3D m in zutiVodovi)
            //{
            //    m.Material = m.BackMaterial;
            //}
        }

        private void Button_Sakrij(object sender, RoutedEventArgs e)
        {
            foreach(GeometryModel3D m in neaktivniDeoMreze)
            {
                MAPA.Children.Remove(m);
            }
            foreach (GeometryModel3D m in secondEnd)
            {
                MAPA.Children.Remove(m);
            }
        }

        private void Button_Prikazi(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in neaktivniDeoMreze)
            {
                MAPA.Children.Add(m);
            }
            foreach (GeometryModel3D m in secondEnd)
            {
               
                MAPA.Children.Add(m);
            }

        }

       
        private void Button_Sakrij01(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in option1)
            {
                MAPA.Children.Remove(m);
            }

        }

        private void Button_Prikazi01(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in option1)
            {
                MAPA.Children.Add(m);
            }

        }

        private void Button_Sakrij12(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in option2)
            {
                MAPA.Children.Remove(m);
            }

        }

        private void Button_Prikazi12(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in option2)
            {
                MAPA.Children.Add(m);
            }

        }

        private void Button_Sakrij2(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in option3)
            {
                MAPA.Children.Remove(m);
            }
        }

        private void Button_Prikazi2(object sender, RoutedEventArgs e)
        {
            foreach (GeometryModel3D m in option3)
            {
                MAPA.Children.Add(m);
            }

        }
    }

    #endregion
}
