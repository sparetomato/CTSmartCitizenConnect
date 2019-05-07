using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace CTSmartCitizenConnect
{
    
    public class SCLocation : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }
        [ConfigurationProperty("SCUserId", IsRequired = true)]
        public string SCUserId
        {
            get
            {
                return (string)this["SCUserId"];
            }
            set
            {
                this["SCUserId"] = value;
            }
        }
        [ConfigurationProperty("SCPassword", IsRequired = true)]
        public string SCPassword
        {
            get
            {
                return (string)this["SCPassword"];
            }
            set
            { this["SCPassword"] = value; }
        }
    }

    [ConfigurationCollection(typeof(SCLocation))]
    public class SCLocationCollection : ConfigurationElementCollection
    {

        internal const string PropertyName = "SCLocation";

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }

        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }



        protected override ConfigurationElement CreateNewElement()
        {
            return new SCLocation();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SCLocation)element).Name;
        }

        public SCLocation this[int idx]
        {
            get
            {
                return (SCLocation)BaseGet(idx);
            }

            set
            {
                if (BaseGet(idx) != null)
                    BaseRemoveAt(idx);

                BaseAdd(idx, value);
            }
        }            

    }

    public class SCLocationsSection : ConfigurationSection
    {
        [ConfigurationProperty("SmartCitizenLocations", IsRequired =true, IsDefaultCollection =true)]
        public SCLocationCollection SmartCitizenLocations
        {
            get
            {
                return (SCLocationCollection)base["SmartCitizenLocations"];
            }
        }
    }

    //public class LocationConfig
    //{
    //    public LocationConfiguartionSection LocationConfiguration
    //    {
    //        get
    //        {
    //            return(LocationConfiguartionSection)ConfigurationManager.GetSection("")
    //        }
    //    }
    //}

    //public class LocationConfigurationElement : ConfigurationElement
    //{
    //    [ConfigurationProperty("name", DefaultValue ="", IsKey =true, IsRequired =true)]
    //    public string Name
    //    {
    //        get
    //        {
    //            return (string)this["name"];
    //        }
    //        set
    //        {
    //            this["name"] = value;
    //        }
    //    }
    //    [ConfigurationProperty("SCUserId", IsRequired =true)]
    //    public string SCUserId
    //    {
    //        get
    //        {
    //            return (string)this["SCUserId"];
    //        }
    //        set
    //        {
    //            this["SCUserId"] = value;
    //        }
    //    }
    //    [ConfigurationProperty("SCPassword", IsRequired =true)]
    //    public string SCPassword
    //    {
    //        get
    //        {
    //            return (string)this["SCPassword"];
    //        }
    //        set
    //        { this["SCPassword"] = value; }
    //    }
    //}

    //[ConfigurationCollection(typeof(LocationConfigurationElement),AddItemName = "SmartCitizenLocations")]
    //public class LocationConfigurationCollection : ConfigurationElementCollection
    //{
    //    protected override ConfigurationElement CreateNewElement()
    //    {
    //        return new LocationConfigurationElement();
    //    }

    //    protected override object GetElementKey(ConfigurationElement element)
    //    {
    //        return ((LocationConfigurationElement)element).Name;
    //    }


    //    public LocationConfigurationElement this[int idx]
    //    {
    //        get
    //        {
    //            return (LocationConfigurationElement)BaseGet(idx);
    //        }
    //        set
    //        {
    //            if (BaseGet(idx) != null)
    //                BaseRemoveAt(idx);
    //            BaseAdd(idx,value);
    //        }
    //    }
    //}

    //public class LocationConfiguartionSection:ConfigurationSection
    //{
    //    //[ConfigurationProperty("SmartCitizenLocationsSection",IsDefaultCollection =true)]
    //    //public LocationConfigurationCollection Locations
    //    //{
    //    //    get
    //    //    {
    //    //        //return (LocationConfigurationCollection)this["SmartCitizenLocationsSection"];
    //    //        return (LocationConfigurationCollection)this["SmartCitizenLocations"];
    //    //    }
    //    //    set
    //    //    {
    //    //        this["SmartCitizenLocationsSection"] = value;
    //    //    }
    //    //}

    //}
}