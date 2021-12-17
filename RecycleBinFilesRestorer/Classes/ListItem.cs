using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecycleBinFilesRestorer.Classes
{
    internal class ListItem<T>
    {
        public String Name { get; set; }
        public T Item { get; set; }

        public ListItem(string name, T item)
        {
            Item = item;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }


}
