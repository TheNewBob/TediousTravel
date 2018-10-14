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
        private const string DATA_PATH = "Assets/StreamingAssets/Mods/TediousData/";
        private const string PORTTOWNS_FILE = "porttowns.xml";

        // Apparently necessary because dictionaries aren't serializable, although they would be a perfect fit for json. Jackson FTW!
        //[Serializable]
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
        public void GeneratePortTownData()
        {
            if (!File.Exists(DATA_PATH))
            {
                System.IO.Directory.CreateDirectory(DATA_PATH);
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

            Debug.Log("number of port towns: " + portTowns.locations.Count);
            var serializer = new XmlSerializer(typeof(PortTowns));
            var stream = new FileStream(DATA_PATH + PORTTOWNS_FILE, FileMode.CreateNew);
            serializer.Serialize(stream, portTowns);
            stream.Close();
        }

        public void LoadPortTowns()
        {
            if (!File.Exists(DATA_PATH + PORTTOWNS_FILE))
            {
                GeneratePortTownData();
            }

            var deserializer = new XmlSerializer(typeof(PortTowns));
            var file = new FileStream(DATA_PATH + PORTTOWNS_FILE, FileMode.Open);
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