using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace TediousTravel
{
    public class TediousData
    {
        private const string PORTTOWNS_FILE = "porttowns.xml";

        // list of additional port towns
        private List<KeyValuePair<string, string>> additionalPorts = 
            new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("Isle of Balfiera", "Blackhead"),
                new KeyValuePair<string, string>("Mournoth", "Zagoparia"),
                new KeyValuePair<string, string>("Betony", "Whitefort"),
                new KeyValuePair<string, string>("Tulune", "The Citadel of Hearthham"),
                new KeyValuePair<string, string>("Tulune", "The Elyzanna Assembly"),
            };


        //Would have prefered json to xml, but apparently JsonUtility is pretty much useless in Unity 2018.
        public class PortTown : object
        {
            public int locationIdx = -1;
            public int regionIdx = -1;

            public PortTown() { }

            public PortTown(int regionIndex, int locationIndex)
            {
                locationIdx = locationIndex;
                regionIdx = regionIndex;
            }
        }

        [XmlRoot("PortTowns")]
        public class PortTowns : object
        {
            [XmlArray("locations")]
            [XmlArrayItem("location")]
            public List<PortTown> locations = new List<PortTown>();
        }

        private static TediousData instance = null;
        public static TediousData Instance {
            get {
                if (instance == null)
                {
                    instance = new TediousData();
                }
                return instance;
            }
        }

        private TediousData() { }



        /**
         * location indices of all port towns on the map, sorted by region index
         */
        private Dictionary<int, List<int>> portTowns = new Dictionary<int, List<int>>();

        /**
         * Exports port towns from BSA files, because you essentially have to load the entire town exterior
         * before you can see whether it's a port town.
         */
        public void GeneratePortTownData(string dataPath)
        {
            if (!File.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }
            DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("Tedious data export complete. This won't be necessary in the future...");

            var reader = DaggerfallUnity.Instance.ContentReader.MapFileReader;
            var portTowns = new PortTowns();
            Debug.Log("Regions: " + reader.RegionCount);

            // walk through all existing locations and check if they're a port town.
            for (int i = 0; i < reader.RegionCount; ++i)
            {
                var region = reader.GetRegion(i);
                Debug.Log("towns in " + region.Name + ": " + region.LocationCount);
                var regionPorts = 0;
                var regionLocations = region.MapNames;
                for (int j = 0; j < region.LocationCount; ++j)
                {
                    var location = reader.GetLocation(region.Name, regionLocations[j]);
                    if (location.Exterior.ExteriorData.PortTownAndUnknown != 0)
                    {
                        portTowns.locations.Add(new PortTown(location.RegionIndex, location.LocationIndex));
                        Debug.Log("port town found in " + region.Name + ": " + location.Name);
                        regionPorts++;
                    }
                }
                Debug.Log("ports in region " + region.Name + ": " + regionPorts);
            }

            AddAditionalPortTowns(portTowns);
            Debug.Log("number of port towns: " + portTowns.locations.Count);
            var serializer = new XmlSerializer(typeof(PortTowns));
            var stream = new FileStream(dataPath + "//" + PORTTOWNS_FILE, FileMode.CreateNew);
            serializer.Serialize(stream, portTowns);
            stream.Close();
        }

        /**
         * Adds additional port towns to the list.
         * These are only port towns in the context of TediousTravel, Daggerfall knows nothing about them.
         */
        private void AddAditionalPortTowns(PortTowns portTowns)
        {
            var reader = DaggerfallUnity.Instance.ContentReader.MapFileReader;
            foreach (KeyValuePair<string, string> i in additionalPorts)
            {
                var location = reader.GetLocation(i.Key, i.Value);
                Debug.Log("Adding " + location.RegionName + ", " + location.Name + " as additional port town: " + location.RegionIndex + ", " + location.LocationIndex);
                portTowns.locations.Add(new PortTown(location.RegionIndex, location.LocationIndex));
            }
        }

        public void LoadPortTowns(string dataPath)
        {
            if (!File.Exists(dataPath + "//" + PORTTOWNS_FILE))
            {
                GeneratePortTownData(dataPath);
            }

            var deserializer = new XmlSerializer(typeof(PortTowns));
            var file = new FileStream(dataPath + "//" + PORTTOWNS_FILE, FileMode.Open);
            var loadedData = deserializer.Deserialize(file) as PortTowns;
            file.Close();

            foreach(var i in loadedData.locations)
            {
                if (!portTowns.ContainsKey(i.regionIdx))
                {
                    portTowns[i.regionIdx] = new List<int>();
                }
                portTowns[i.regionIdx].Add(i.locationIdx);
            }
        }

        public bool IsPortTown(int regionIdx, int locationIdx)
        {
            try
            {
                return portTowns[regionIdx].Contains(locationIdx);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}