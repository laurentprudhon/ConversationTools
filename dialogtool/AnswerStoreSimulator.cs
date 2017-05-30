using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace dialogtool
{
    public class AnswerStoreSimulator
    {
        private const string ANSWERS_DIR = @".\answers\";

        public AnswerStoreSimulator(string answerUnitsFile, string answersDir = ANSWERS_DIR)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader streamReader = new StreamReader(answersDir + answerUnitsFile))
            {
                AnswerUnits = (List<AnswerUnit>)serializer.Deserialize(streamReader, typeof(List<AnswerUnit>));
                // DEBUG <-- TO COMMENT
                //using (StreamWriter sw = new StreamWriter(ANSWERS_DIR + answerUnitsFile + ".mappings.txt"))
                //{
                //    var mappingUris = AnswerUnits.Where(au => au.mappingInfo != null).SelectMany(au => au.mappingInfo).Where(mi => mi.mappingURI != null).Select(mi => mi.mappingURI).Distinct().OrderBy(s => s);
                //    foreach (var mappingUri in mappingUris)
                //    {
                //        sw.WriteLine(mappingUri);
                //    }
                //}
                // DEBUG -->
            }
        }

        public List<AnswerUnit> AnswerUnits { get; private set; }

        public AnswerUnit GetAnswerUnitForMappingUri(string mappingUri)
        {
            var answerUnit = AnswerUnits.Where(au => au.mappingInfo != null && au.mappingInfo.Where(mi => mi.mappingURI == mappingUri).Any()).FirstOrDefault();
            return answerUnit;
        }
    }

    public class AnswerUnit
    {
        public string answerUnitID { get; set; }
        public content content { get; set; }
        public string evidenceURL { get; set; }
        public int indexInSourceDocument { get; set; }
        public List<mappingInfo> mappingInfo { get; set; }
        public List<metadata> metadata { get; set; }
        public sourceDocument SourceDocument { get; set; }
    }

    public class content
    {
        public string contentID { get; set; }
        public string hashcode { get; set; }
        public string plainText { get; set; }
        public List<string> title { get; set; }
        public string type { get; set; }
        public string htmlText { get; set; }
    }

    public class title
    {
        public string titles { get; set; }
    }

    public class mappingInfo
    {
        public string comment { get; set; }
        public string getMappingURI { get; set; }
        public string mappingURI { get; set; }
        public string intent { get; set; }
        public List<mappingEntities> mappingEntities { get; set; }
        public string status { get; set; }
        public string validatedBy { get; set; }
        public string validatedOn { get; set; }
    }

    public class mappingEntities
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class metadata
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class sourceDocument
    {
        public string ingestionDate { get; set; }
        public string sourceDocumentID { get; set; }
        public string sourceID { get; set; }
        public string type { get; set; }
        public string validFrom { get; set; }
        public string version { get; set; }
        public List<metadata> metadata { get; set; }
    }
}
