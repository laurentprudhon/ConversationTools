using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace dialogtool
{
    class EntityValuesMatcher
    {
        public static string ReplaceTextWithConcepts(string originalText, IDictionary<string, ConceptGroupWithTheSameSynonym> conceptsSynonyms, Regex conceptsRegex, out IDictionary<int[],ConceptSubstitution> conceptSubstitutions)
        {
            var textReplacedWithConcepts = originalText;
            int replacementOffset = 0;
            conceptSubstitutions = null;

            var matches = conceptsRegex.Matches(originalText);
            if (matches.Count > 0)
            {
                conceptSubstitutions = new Dictionary<int[], ConceptSubstitution>();
                foreach (var obj in matches)
                {
                    var match = (Match)obj;
                    int startIndex = match.Index;
                    int stopIndex = startIndex + match.Length - 1;

                    var synonym = match.Value;
                    var replacementConceptGroup = conceptsSynonyms[synonym];
                    string replacementText = null;
                    int[] valuesIndexesAfterReplacement = null;
                    if (replacementConceptGroup.Concepts.Count() == 1)
                    {
                        replacementText = replacementConceptGroup.Concepts.First().CanonicalValue;
                        valuesIndexesAfterReplacement = new int[] { startIndex };
                    }
                    else
                    {
                        valuesIndexesAfterReplacement = new int[1 + replacementConceptGroup.Concepts.Count()];
                        int i = 0;
                        int localOffset = startIndex + replacementOffset;

                        replacementText = "{" + synonym;
                        localOffset += 1;
                        valuesIndexesAfterReplacement[i++] = localOffset;
                        localOffset += synonym.Length;
                        foreach (var concept in replacementConceptGroup.Concepts)
                        {
                            replacementText += "|" + concept.CanonicalValue;
                            localOffset += 1;
                            valuesIndexesAfterReplacement[i++] = localOffset;
                            localOffset += concept.CanonicalValue.Length;
                        }
                        replacementText += "}";

                    }

                    if (replacementText != synonym)
                    {
                        var conceptSubstitution = new ConceptSubstitution(originalText, startIndex, stopIndex, replacementConceptGroup, replacementText);
                        var newStartIndexAfterReplacement = startIndex + replacementOffset;
                        conceptSubstitutions.Add(valuesIndexesAfterReplacement, conceptSubstitution);

                        var beforeSubstitution = textReplacedWithConcepts.Substring(0, newStartIndexAfterReplacement);
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
            // Remove accented characters before the matching process
            var textWithoutAccentedChars = StringUtils.RemoveDiacritics(originalText);

            IDictionary<int[],ConceptSubstitution> conceptSubstitutions;
            var textReplacedWithConcepts = ReplaceTextWithConcepts(textWithoutAccentedChars, conceptsSynonyms, conceptsRegex, out conceptSubstitutions);
                        
            IList<EntityValueMatch> entityValueMatches = new List<EntityValueMatch>();
            IList<EntityValue> entityValues = new List<EntityValue>();
            foreach (var entity in entities)
            {
                var matches = entity.EntityValuesRegex.Matches(textReplacedWithConcepts);
                ISet<int> substitutionsAlreadyMatched = new HashSet<int>();
                foreach(Match match in matches)
                {
                    // If match found after substitution, make sure we only match one concept for a given entity
                    if(conceptSubstitutions != null)
                    {
                        ConceptSubstitution conceptSubstitution = null;
                        foreach (var valuesIndexesAfterReplacement in conceptSubstitutions.Keys)
                        {
                            if(valuesIndexesAfterReplacement[0] > match.Index)
                            {
                                break;
                            }
                            if (valuesIndexesAfterReplacement.Contains(match.Index))
                            {
                                conceptSubstitution = conceptSubstitutions[valuesIndexesAfterReplacement];
                                break;
                            }
                        }
                        if(conceptSubstitution != null)
                        {
                            var originalStartIndex = conceptSubstitution.StartIndex;
                            if (substitutionsAlreadyMatched.Contains(originalStartIndex))
                            {
                                continue;
                            }
                            else
                            {
                                substitutionsAlreadyMatched.Add(originalStartIndex);
                            }
                        }
                    }

                    var entityValueText = match.Value;
                    var entityValue = entity.TryGetEntityValue(entityValueText);
                    if(entityValue == null)
                    {
                        entityValue = entity.TryGetEntityValueFromConcept(entityValueText);
                    }
                    entityValues.Add(entityValue);
                    var entityValueMatch = new EntityValueMatch(textReplacedWithConcepts, match.Index, match.Index + match.Length - 1, entityValue);
                    entityValueMatches.Add(entityValueMatch);
                }
            }
            
            return new EntityValuesMatchResult(originalText, conceptSubstitutions != null ? conceptSubstitutions.Values.ToList() : null, textReplacedWithConcepts, entityValueMatches, entityValues);
        }
    }
}
