using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using CAP1_Constantes;
using CAP1_Xml;


namespace SEA
{
    public class CallbackCfg
    {
        public string Url { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

    public static class ReceiveInfoAux
    {

        public static CallbackCfg GetCallbackCfg(CDocumentoXML callbackXml)
        {
            var callback = XElement.Parse(callbackXml.InnerXml);
            var url = callback.Element("url");
            if (url == null) { return new CallbackCfg { Url = string.Empty, Parameters = new Dictionary<string, string>() }; }
            var result = new CallbackCfg
            {
                Url = url.Value,
                Parameters = (from p in callback.Descendants("parameters").Descendants("parameter")
                              let name = p.Element("name")
                              let value = p.Element("value")
                              where name != null && value != null
                              select new { Key = name.Value, value.Value }).ToDictionary(x => x.Key, x => x.Value)
            };
            return result;
        }

        public static XElement GetParamXml(CallbackCfg callbackCfg)
        {
            return new XElement(CConstantesXML.ROOT, new XElement("parameters",
                from p in callbackCfg.Parameters
                select new XElement("parameter",
                    new XElement("name", p.Key),
                    new XElement("value", p.Value))));
        }

        
    }
}