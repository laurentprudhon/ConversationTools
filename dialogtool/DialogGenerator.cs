using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dialogtool
{
    class DialogGenerator
    {
        /*
        private static void GenerateLogicFromMappingUris(IEnumerable<SelectableMappingUri> selectableMappingURIs, StreamWriter sw, string indent, bool displayIntent)
        {
            var selectivities = new Dictionary<string, IDictionary<Tuple<string, string>, List<SelectableMappingUri>>>();
            foreach (var selectableMappingUri in selectableMappingURIs)
            {
                var mappingUri = selectableMappingUri.MappingUri;
                if (String.IsNullOrEmpty(mappingUri)) continue;
                
                string[] segments = mappingUri.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string intent = segments[1];

                IDictionary<Tuple<string, string>, List<SelectableMappingUri>> selectivitiesForIntent = null;
                if (selectivities.ContainsKey(intent))
                {
                    selectivitiesForIntent = selectivities[intent];
                }
                else
                {
                    selectivitiesForIntent = new Dictionary<Tuple<string, string>, List<SelectableMappingUri>>();
                    selectivities[intent] = selectivitiesForIntent;
                }

                for (int i = 2; i < segments.Length; i += 2)
                {
                    var entityNameValue = Tuple.Create(mappingUriValues[i], segments[i + 1]);

                    if (!selectivitiesForIntent.ContainsKey(entityNameValue))
                    {
                        selectivitiesForIntent.Add(entityNameValue, new List<SelectableMappingUri>());
                    }
                    selectivitiesForIntent[entityNameValue].Add(selectableMappingUri);
                }
            }

            foreach (var intent in selectivities.Keys)
            {
                if (displayIntent) sw.WriteLine(intent);
                var selectivitiesForIntent = selectivities[intent];
                var mostSelectiveEntities = selectivitiesForIntent.GroupBy(keyValuePair => keyValuePair.Key.Item1).OrderBy(group => group.Average(keyValuePair => keyValuePair.Value.Count));
                foreach (var entityValueGroup in mostSelectiveEntities)
                {
                    if (entityValueGroup.Sum(entityKeyValuePair => entityKeyValuePair.Value.Where(selectableURI => !selectableURI.Selected).Count()) > 0)
                    {
                        sw.WriteLine(indent + "   >" + entityValueGroup.Key + "?");
                    }
                    foreach (var entityKeyValuePair in entityValueGroup.OrderBy(keyValuePair => keyValuePair.Value.Count))
                    {
                        var selectableUris = entityKeyValuePair.Value.Where(selectableURI => !selectableURI.Selected);
                        var selectableURIsCount = selectableUris.Count();
                        if (selectableURIsCount > 0)
                        {
                            sw.WriteLine(indent + "      \"" + entityKeyValuePair.Key.Item2 + "\"");
                            if (selectableURIsCount == 1)
                            {
                                foreach (var mappingUri in selectableUris)
                                {
                                    sw.WriteLine(indent + "         " + mappingUri.MappingUri);
                                    mappingUri.Selected = true;
                                }
                            }
                            else
                            {
                                GenerateLogicFromMappingUris(selectableUris, sw, indent + "      ", false);
                            }
                        }
                    }
                }
                if (displayIntent) sw.WriteLine();
            }
        }

        public class SelectableMappingUri
        {
            public SelectableMappingUri(string mappingUri)
            {
                MappingUri = mappingUri;
                Selected = false;
            }

            public string MappingUri;
            public bool Selected;
        }
        */
    }
}
