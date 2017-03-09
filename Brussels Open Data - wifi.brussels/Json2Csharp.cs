// •————————————————————————————————————————————————————————————————————————————————————————————————————————————•
// |                                                                                                            |
// |    brussels open data - wifi.brussels is a proof of concept (PoC). <https://github.com/neojudgment/>       |
// |                                                                                                            |
// |    wifi.brussels is a free Wi-Fi network covering certain zones of the territory of                        |
// |    the territory of the Brussels-Capital Region in Belgium.                                                |
// |                                                                                                            |
// |    brussels open data - wifi.brussels uses Microsoft WindowsAPICodePack and GMap.NET to                    |
// |    demonstrate how to retrieve data from Brussels open data Store.                                         |
// |                                                                                                            |
// |    brussels open data - wifi.brussels uses Elysium library that implements Modern UI for                   |
// |    Windows Presentation Foundation.                                                                        |
// |                                                                                                            |
// |    This program is under Microsoft Public License (Ms-RL)                                                  |
// |                                                                                                            |
// |    This program is distributed in the hope that it will be useful but WITHOUT ANY WARRANTY;                |
// |    without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.               |
// |                                                                                                            |
// |    You should have received a copy of the Microsoft Public License (Ms-RL)                                 |
// |    along with this program.  If not, see <http://opensource.org/licenses/MS-RL>.                           |
// |                                                                                                            |
// |    Copyright © Pascal Hubert - Brussels, Belgium 2017. <mailto:pascal.hubert@outlook.com>                  |
// •————————————————————————————————————————————————————————————————————————————————————————————————————————————•

using System.Collections.Generic;

namespace OpenData
{
    public class Json2Csharp
    {
        public class Crs
        {
            public Properties2 Properties { get; set; }
            public string Type { get; set; }
        }

        public class Feature
        {
            private int i;

            public Feature(int i)
            {
                this.i = i;
            }

            public Geometry Geometry { get; set; }
            public string Geometry_name { get; set; }
            public string Id { get; set; }
            public Properties Properties { get; set; }
            public string Type { get; set; }
        }

        public class Geometry
        {
            public List<double> Coordinates { get; set; }
            public string Type { get; set; }
        }

        public class Properties
        {
            public int? AP_COUNT { get; set; }
            public string CLIENT { get; set; }
            public string DATE_REALISATION { get; set; }
            public int ID { get; set; }
            public string ID_EXTERNE { get; set; }
            public string INTERV_CIRB { get; set; }
            public string LIEU_INSTALLATION { get; set; }
            public string NOM_SITE { get; set; }
            public object PARTICIP_CIRB { get; set; }
            public object REF { get; set; }
            public string STATUT { get; set; }
            public string TYPE_CLIENT { get; set; }
        }

        public class Properties2
        {
            public string Code { get; set; }
        }

        public class RootObject
        {
            public List<double> Bbox { get; set; }
            public Crs Crs { get; set; }
            public List<Feature> Features { get; set; }
            public string Type { get; set; }
        }
    }
}