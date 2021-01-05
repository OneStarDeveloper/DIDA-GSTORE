using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    //this class represents a summary information about the servers, to help us in the mapping (both in servers and clients)*
    //*because everyone needs to have information regarding the servers (and some info about them) we decided it made sense*
    //*to have a separated class with the most important information shared among them
    public class PlaceholderServer
    {
        private string server_id;
        private string url;
        private bool is_master;
        
        //this property checks whether a server is up (running) or down (crashed)
        private bool status=true;

        public PlaceholderServer(string server_id, string url, bool is_master)
        {
            this.server_id = server_id;
            this.url = url;
            this.is_master = is_master;
        }

        public string Server_id
        {
            get { return this.server_id; }
        }

        public string Url
        {
            get { return this.url; }
        }

        public bool Is_master
        {
            get { return this.is_master; }
            set { this.is_master = value; }
        }

        public bool Status
        {
            get { return this.status; }
            set { this.status = value; }
        }

    }
}
