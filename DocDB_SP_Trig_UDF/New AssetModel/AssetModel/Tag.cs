using System;
using System.Collections.Generic;
using System.Text;

namespace AssetModel
{
    public class Tag
    {
        public int TagId
        {
            get;
            set;

        }
        public string TagName
        {
            get;
            set;

        }
        public string EnergyType
        {
            get;
            set;

        }
        public string UOM
        {
            get;
            set;

        }

        public TagDetails TagDetails { get; set; }
    }
}
