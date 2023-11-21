using System.ComponentModel;

namespace Utilities
{
    public class NameListIDPair : INotifyPropertyChanged
    {
        private string _listid;
        private string _name;

        public event PropertyChangedEventHandler PropertyChanged;

        public NameListIDPair(string ListID, string Name)
        {
            _listid = ListID;
            _name = Name;
        }

        public NameListIDPair()
        {
            _listid = "";
            _name = "";
        }
        public string ListID
        {
            get
            {
                return _listid;
            }
            set
            {
                _listid = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ListID"));
            }
        }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }
        public override string ToString()
        {
            return string.Format("\"{0}\",\"{1}\"", _listid, _name);
        }
    }

    public class NameIDPair : INotifyPropertyChanged
    {
        private int _id;
        private string _name;

        public event PropertyChangedEventHandler PropertyChanged;

        public NameIDPair(int ID, string Name)
        {
            _id = ID;
            _name = Name;
        }

        public NameIDPair()
        {
            _id = 0;
            _name = "";
        }
        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ID"));
            }
        }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public override string ToString()
        {
            return string.Format("\"{0}\",\"{1}\"", _id, _name);
        }
    }

    public static class NamePairExtensions
    {
        public static string ToFormattedString(this NameListIDPair pair)
        {
            return pair.ListID + "|" + pair.Name;
        }

        public static string ToFormattedString(this NameIDPair pair)
        {
            return pair.ID.ToString() + "|" + pair.Name;
        }
    }

}
