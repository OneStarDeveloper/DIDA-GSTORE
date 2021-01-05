using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Server
{
    //this class represents an objects stored by the server
    public class DataStoreObject
    {
        private string partition_id;
        private string object_id;
        private string value;
        private (string, string) object_key;
        private bool is_locked_object=false;
        
        public DataStoreObject(string partition_id, string object_id, string value)
        {
            this.partition_id = partition_id;
            this.object_id = object_id;
            this.object_key = (partition_id,object_id);
            this.value = value;
        }

        public string Partition_id
        {
            get { return partition_id; }
        }
        public string Object_id
        {
            get { return object_id; }
        }

        public (string,string) Object_key
        {
            get { return this.object_key; }
        }

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public bool Is_locked_object
        {
            get { return this.is_locked_object; }
            set { this.is_locked_object = value; }
        }
    }
}
