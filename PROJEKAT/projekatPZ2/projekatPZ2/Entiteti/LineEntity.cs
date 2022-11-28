using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace projekatPZ2.Entiteti
{
    public enum MaterijalVoda { Copper, Steel,Acsr,Other }
    public class LineEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long FirstEnd { get; set; }
        public long SecondEnd { get; set; }
        public double Otpornost { get; set; }

        public MaterijalVoda MaterijalVoda { get; set; }
    

        public LineEntity(long id, string name, long firstEnd, long secondEnd)
        {
            Id = id;
            Name = name;
            FirstEnd = firstEnd;
            SecondEnd = secondEnd;
            
        }

        public override string ToString()
        {
            return $"Line:\nID: {Id}\nStart:{FirstEnd}\nEnd:{SecondEnd}";
        }

        public List<Point> TackeLinije { get; set; }
        public LineEntity()
        {
            TackeLinije = new List<Point>();
        }
    }
}
