using System;
using System.Runtime.Serialization;

namespace WS_CreateJob
{
    [DataContract]
    public class XmlData
    {
        string _document = string.Empty;
        int _status = 1;

        public XmlData()
        {
            MaxAttempt = 0;
            Attempt = 0;
        }

        [DataMember]
        public string Document
        {
            get { return _document; }
            set { _document = value; }
        }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public int Status
        {
            get { return _status; }
            set { _status = value; }
        }

        [DataMember]
        public int Attempt { get; set; }

        [DataMember]
        public int MaxAttempt { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}