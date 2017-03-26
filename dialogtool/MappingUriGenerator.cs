using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dialogtool
{
    public static class MappingUriGenerator
    {
        public enum MappingUriConfig
        {
            Insurance,
            Savings
        }

        // Insurance configuration

        private static string Insurance_RedirectToLongTailVariable = "REDIRECT_LONG_TAIL";

        private static string[][] Insurance_DeduceVariableValues = {
            new string[] { "CLASSIFIER_CLASS_0", "Housing_", "SubDomain_Var", "housing" },
            new string[] { "CLASSIFIER_CLASS_0", "Auto_", "SubDomain_Var", "auto" } };

        private static string[][] Insurance_MappingUriSegments = {
            new string[] { "intent", "CLASSIFIER_CLASS_0" },
            new string[] { "subdomain_entity", "SubDomain_Var" },
            new string[] { "object_entity", "Object_Var" },
            new string[] { "event_entity", "Event_Var" },
            new string[] { "person_entity", "Person_Var" },
            new string[] { "product_entity", "Product_Var" },
            new string[] { "guarantee_entity", "Guarantee_Var" } };

        // Savings configuration
        
        private static string Savings_RedirectToLongTailVariable = "REDIRECT_LONG_TAIL";

        private static string[][] Savings_DeduceVariableValues = {
            new string[] { "CLASSIFIER_CLASS_0", "Housing_", "SubDomain_Var", "housing" },
            new string[] { "CLASSIFIER_CLASS_0", "Auto_", "SubDomain_Var", "auto" } };

        private static string[][] Savings_MappingUriSegments = {
            new string[] { "federationGroup", "federationGroup" },
            new string[] { "intent", "CLASSIFIER_CLASS_0" },
            new string[] { "subdomain_entity", "SubDomain_Var" },
            new string[] { "support_entity", "Support_Var" },
            new string[] { "event_entity", "Event_Var" },
            new string[] { "person_entity", "Person_Var" },
            new string[] { "product_entity", "Product_Var" },
            new string[] { "history_entity", "History_Var" } };

        // Expose config

        internal static IEnumerable<string> GetEntityVariables(MappingUriConfig mappingUriConfig)
        {
            string[][] mappingUriSegments = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_MappingUriSegments : Savings_MappingUriSegments;
            return mappingUriSegments.Where(p => p[1] != "federationGroup" && p[1] != "CLASSIFIER_CLASS_0").SelectMany(p => new string[] { p[1], p[1] + "_2" });
        }

        // Mapping URIs generation

        public static string[] GenerateMappingURIs(DialogVariablesSimulator dialogVariablesSimulator, MappingUriConfig mappingUriConfig, out bool redirectToLongTail)
        {
            // Redirect to long tail ?
            var redirectToLongTailVariableName = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_RedirectToLongTailVariable : Savings_RedirectToLongTailVariable;
            var redirectToLongTailValue = dialogVariablesSimulator.TryGetVariableValue(redirectToLongTailVariableName);
            redirectToLongTail = redirectToLongTailValue == "yes";
            if(redirectToLongTail)
            {
                return null;
            }

            // Deduce subdomain from entity name
            string[][] deduceVariableValues = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_DeduceVariableValues : Savings_DeduceVariableValues;
            foreach(var deduceVariableStrings in deduceVariableValues)
            {
                var inspectedVariable = deduceVariableStrings[0];
                var searchPattern = deduceVariableStrings[1];
                var targetVariable = deduceVariableStrings[2];
                var targetValue = deduceVariableStrings[3];

                var inspectedValue = dialogVariablesSimulator.TryGetVariableValue(inspectedVariable);
                if(inspectedValue != null && inspectedValue.Contains(searchPattern))
                {
                    dialogVariablesSimulator.SetVariableValue(targetVariable, targetValue, DialogNodeType.FatHeadAnswers);
                }
            }

            // Generate mapping URIs
            string[][] mappingUriSegments = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_MappingUriSegments : Savings_MappingUriSegments;
            IList<string>[] mappingUriValues = new IList<string>[mappingUriSegments.Length];
            for(int i = 0; i < mappingUriSegments.Length; i++)
            {
                mappingUriValues[i] = dialogVariablesSimulator.TryGetVariableValues(mappingUriSegments[i][1]);
            }
            var indexes = new int[mappingUriSegments.Length];
            int count = 0;
            for (int i = 0; i < mappingUriSegments.Length; i++)
            {
                if (mappingUriValues[i] != null)
                {
                    if (count == 0) count = mappingUriValues[i].Count;
                    else count *= mappingUriValues[i].Count;
                }
                else
                {
                    indexes[i] = -1;
                }
            }
            var result = new string[count];
            for (int cnt = 0; cnt < count; cnt++)
            {
                for (int i = 0; i < mappingUriSegments.Length; i++)
                {
                    if (indexes[i] >= 0)
                    {
                        result[cnt] += "/" + mappingUriSegments[i][0] + "/" + mappingUriValues[i][indexes[i]];
                    }
                }
                if (cnt < (count - 1))
                {
                    int idx = 0;
                    for (;;)
                    {
                        if (indexes[idx] < 0)
                        {
                            idx++;
                            continue;
                        }
                        if (indexes[idx] < (mappingUriValues[idx].Count - 1))
                        {
                            indexes[idx]++;
                            break;
                        }
                        else
                        {
                            indexes[idx] = 0;
                            idx++;
                        }
                    }
                }
            }
            return result;
        }

        public static string ComputeMappingURI(IDictionary<string, string> variablesValues, MappingUriConfig mappingUriConfig, out bool redirectToLongTail)
        {
            // Redirect to long tail ?
            var redirectToLongTailVariableName = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_RedirectToLongTailVariable : Savings_RedirectToLongTailVariable;
            string redirectToLongTailValue = null;
            variablesValues.TryGetValue(redirectToLongTailVariableName, out redirectToLongTailValue);
            redirectToLongTail = redirectToLongTailValue == "yes";
            if (redirectToLongTail)
            {
                return null;
            }

            // Deduce subdomain from entity name
            string[][] deduceVariableValues = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_DeduceVariableValues : Savings_DeduceVariableValues;
            foreach (var deduceVariableStrings in deduceVariableValues)
            {
                var inspectedVariable = deduceVariableStrings[0];
                var searchPattern = deduceVariableStrings[1];
                var targetVariable = deduceVariableStrings[2];
                var targetValue = deduceVariableStrings[3];

                string inspectedValue = null;
                variablesValues.TryGetValue(inspectedVariable, out inspectedValue);
                if (inspectedValue != null && inspectedValue.Contains(searchPattern))
                {
                    variablesValues[targetVariable] = targetValue;
                }
            }

            // Generate mapping URIs
            string[][] mappingUriSegments = mappingUriConfig == MappingUriConfig.Insurance ? Insurance_MappingUriSegments : Savings_MappingUriSegments;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < mappingUriSegments.Length; i++)
            {
                string mappingUriValue = null;
                if (variablesValues.TryGetValue(mappingUriSegments[i][1], out mappingUriValue))
                {
                    if (!String.IsNullOrEmpty(mappingUriValue))
                    {
                        sb.Append('/');
                        sb.Append(mappingUriSegments[i][0]);
                        sb.Append('/');
                        sb.Append(mappingUriValue);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
