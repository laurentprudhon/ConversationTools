using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace dialogtool
{
    class EntityValuesMatcher
    {
        public static string ReplaceTextWithConcepts(string originalText, IDictionary<string, ConceptGroupWithTheSameSynonym> conceptsSynonyms, Regex conceptsRegex, out IList<ConceptSubstitution> conceptSubstitutions)
        {
            var textReplacedWithConcepts = originalText;
            int replacementOffset = 0;
            conceptSubstitutions = null;

            var matches = conceptsRegex.Matches(originalText);
            if (matches.Count > 0)
            {
                conceptSubstitutions = new List<ConceptSubstitution>();
                foreach (var obj in matches)
                {
                    var match = (Match)obj;
                    int startIndex = match.Index;
                    int stopIndex = startIndex + match.Length - 1;

                    var synonym = match.Value;
                    var replacementConceptGroup = conceptsSynonyms[synonym];
                    string replacementText = null;
                    if (replacementConceptGroup.Concepts.Count() == 1)
                    {
                        replacementText = replacementConceptGroup.Concepts.First().CanonicalValue;
                    }
                    else
                    { 
                        replacementText = "{" + synonym;
                        foreach (var concept in replacementConceptGroup.Concepts)
                        {
                            replacementText += "|" + concept.CanonicalValue;
                        }
                        replacementText += "}";

                    }

                    if (replacementText != synonym)
                    {
                        var conceptSubstitution = new ConceptSubstitution(originalText, startIndex, stopIndex, replacementConceptGroup, replacementText);
                        conceptSubstitutions.Add(conceptSubstitution);

                        var beforeSubstitution = textReplacedWithConcepts.Substring(0, startIndex + replacementOffset);
                        var afterSubstitution = textReplacedWithConcepts.Substring(stopIndex + replacementOffset + 1, textReplacedWithConcepts.Length - stopIndex - replacementOffset - 1);
                        textReplacedWithConcepts = beforeSubstitution + replacementText + afterSubstitution;
                        replacementOffset += replacementText.Length - (stopIndex - startIndex + 1);
                    }
                }
            }

            return textReplacedWithConcepts;
        }

        public static EntityValuesMatchResult MatchEntityValues(IEnumerable<Entity> entities, string originalText, IDictionary<string,ConceptGroupWithTheSameSynonym> conceptsSynonyms, Regex conceptsRegex)
        {
            IList<ConceptSubstitution> conceptSubstitutions;
            var textReplacedWithConcepts = ReplaceTextWithConcepts(originalText, conceptsSynonyms, conceptsRegex, out conceptSubstitutions);

            IList<EntityValueMatch> entityValueMatches = new List<EntityValueMatch>();
            IList<EntityValue> entityValues = new List<EntityValue>();
            foreach (var entity in entities)
            {
                var matches = entity.EntityValuesRegex.Matches(textReplacedWithConcepts);
                foreach(Match match in matches)
                {
                    var entityValueText = match.Value;
                    var entityValue = entity.TryGetEntityValue(entityValueText);
                    if(entityValue == null)
                    {
                        entityValue = entity.TryGetEntityValueFromSynonym(entityValueText);
                    }
                    entityValues.Add(entityValue);
                    var entityValueMatch = new EntityValueMatch(textReplacedWithConcepts, match.Index, match.Index + match.Length - 1, entityValue);
                    entityValueMatches.Add(entityValueMatch);
                }
            }
            
            return new EntityValuesMatchResult(originalText, conceptSubstitutions, textReplacedWithConcepts, entityValueMatches, entityValues);
        }
    }
}
