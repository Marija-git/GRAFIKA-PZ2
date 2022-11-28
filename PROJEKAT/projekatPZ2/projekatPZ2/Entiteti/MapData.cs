using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projekatPZ2.Entiteti;

namespace projekatPZ2.Entiteti
{
   public class MapData
    {
        private int sirinaX;
        private int visinaY;

        public PowerEntity[,] entitet;
        public LineEntity[,] linija;
      

        public MapData(int x,int y)
        {
            sirinaX = x;
            visinaY = y;

            Entitet = new PowerEntity[sirinaX ,visinaY ];
            Linija = new LineEntity[sirinaX, visinaY];

            KolekcijaEntiteta = new Dictionary<long, PowerEntity>();
            KolekcijaLinija = new Dictionary<long, LineEntity>();
          
        }


        public PowerEntity[,] Entitet { get => entitet; set => entitet = value; }
        public LineEntity[,] Linija { get => linija; set => linija = value; }
        public Dictionary<long, PowerEntity> KolekcijaEntiteta { get; set; }
        public Dictionary<long, LineEntity> KolekcijaLinija { get; set; }
    }
}
