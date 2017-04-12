using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace dialogtool
{
    public class Entity
    {
        public Entity(string name)
        {
            Name = name;
            Values = new List<EntityValue>();
        }

        public string Name { get; private set; }
        public IList<EntityValue> Values { get; private set; }

        public IDictionary<string, EntityValue> EntityValueNamesDictionary { get; private set; }
        public IDictionary<string, EntityValue> EntityValuesDictionary { get; private set; }
        public IDictionary<string, EntityValue> EntityValueFromConceptDictionary { get; private set; }
        public IDictionary<string, EntityValue> EntityValueSynonymsDictionary { get; private set; }
        internal Regex EntityValuesRegex { get; private set; }

        internal void AddEntityValue(EntityValue entityValue, IMessageCollector errors)
        {
            Values.Add(entityValue);
        }

        public void OnAllEntityValuesAdded(IMessageCollector errors)
        {
            // Entity value names comparison isn't case sensitive
            EntityValueNamesDictionary = new Dictionary<string, EntityValue>(StringComparer.InvariantCultureIgnoreCase);
            // Canonical values comparison isn't case sensitive
            EntityValuesDictionary = new Dictionary<string, EntityValue>(StringComparer.InvariantCultureIgnoreCase);
            // Synonyms from concepts comparison isn't case sensitive
            EntityValueFromConceptDictionary = new Dictionary<string, EntityValue>(StringComparer.InvariantCultureIgnoreCase);
            EntityValueSynonymsDictionary = new Dictionary<string, EntityValue>(StringComparer.InvariantCultureIgnoreCase);
            
            foreach (var entityValue in Values)
            {
                if (!EntityValueNamesDictionary.ContainsKey(entityValue.Name))
                {
                    EntityValueNamesDictionary.Add(entityValue.Name, entityValue);
                }
                else
                {
                    var otherEntityValue = EntityValueNamesDictionary[entityValue.Name];
                    errors.LogMessage(entityValue.LineNumber, MessageType.DuplicateKey, "Entity " + Name + " - two entity values declared with the same name \"" + entityValue.Name + "\" : line " + entityValue.LineNumber + " " + entityValue.CanonicalValue + ", and line " + otherEntityValue.LineNumber + " " + otherEntityValue.CanonicalValue);
                }
                if (!EntityValuesDictionary.ContainsKey(entityValue.CanonicalValue))
                {
                    EntityValuesDictionary.Add(entityValue.CanonicalValue, entityValue);
                }
                else
                {
                    var otherEntityValue = EntityValuesDictionary[entityValue.CanonicalValue];
                    errors.LogMessage(entityValue.LineNumber, MessageType.DuplicateKey, "Entity " + Name + " - two entity values declared with the same value \""+ entityValue.CanonicalValue + "\" : line " + entityValue.LineNumber + " " + entityValue.Name + ", and line " + otherEntityValue.LineNumber + " " + otherEntityValue.Name);
                }
                if(entityValue.Concept != null)
                {
                    var conceptCanonicalValue = entityValue.Concept.CanonicalValue;                    
                    if (!EntityValueFromConceptDictionary.ContainsKey(conceptCanonicalValue))
                    {
                        EntityValueFromConceptDictionary.Add(conceptCanonicalValue, entityValue);
                    }
                    foreach (var synonym in entityValue.Concept.Synonyms)
                    {
                        if (!EntityValueSynonymsDictionary.ContainsKey(synonym))
                        {
                            EntityValueSynonymsDictionary.Add(synonym, entityValue);
                        }
                        else
                        {
                            var conflictingEntityValue = EntityValueSynonymsDictionary[synonym];
                            if (entityValue.Name != conflictingEntityValue.Name)
                            {
                                errors.LogMessage(entityValue.LineNumber, MessageType.DuplicateKey, "Entity " + Name + " : concept synonym \"" + synonym + "\" defined for entity value \"" + entityValue.Name + "\" is conflicting with an identical concept synonym previoulsy defined for another entity value of the same entity \"" + conflictingEntityValue.Name + "\" on line " + conflictingEntityValue.LineNumber);
                            }
                        }
                    }
                }
            }

            // Generate regular expression to match entity values
            StringBuilder regexBuilder = new StringBuilder();
            regexBuilder.Append("\\b(");
            bool isFirstValue = true;
            foreach(var entityValue in EntityValuesDictionary.Values.OrderByDescending(ev => ev.CanonicalValue.Length))
            {
                if(isFirstValue)
                {
                    isFirstValue = false;
                }
                else
                {
                    regexBuilder.Append("|");
                }
                regexBuilder.Append(Regex.Escape(entityValue.CanonicalValue));
                if(entityValue.Concept != null)
                {
                    regexBuilder.Append("|");
                    regexBuilder.Append(Regex.Escape(entityValue.Concept.CanonicalValue));
                }
            }
            regexBuilder.Append(")\\b");
            EntityValuesRegex = new Regex(regexBuilder.ToString(), RegexOptions.IgnoreCase);
        }

        public EntityValue TryGetEntityValueFromName(string name)
        {
            if (EntityValueNamesDictionary == null) return null;
            else
            {
                EntityValue entityValue = null;
                if (EntityValueNamesDictionary.TryGetValue(name, out entityValue))
                {
                    return entityValue;
                }
                else
                {
                    return null;
                }
            }
        }

        public EntityValue SuggestEntityValueFromName(string approximateName, IEnumerable<Entity> entities)
        {
            var entityValue = SuggestEntityValueFromName(approximateName, this);
            if (entityValue != null)
            {
                return entityValue;
            }
            else
            {
                // Try in other entities
                foreach (var otherEntity in entities)
                {
                    if (otherEntity != this)
                    {
                        if (otherEntity.EntityValuesDictionary.ContainsKey(approximateName))
                        {
                            return otherEntity.EntityValuesDictionary[approximateName];
                        }
                        else
                        {
                            entityValue = SuggestEntityValueFromName(approximateName, otherEntity);
                            if (entityValue != null)
                            {
                                return entityValue;
                            }
                        }
                    }
                }
                return null;
            }
        }

        private static EntityValue SuggestEntityValueFromName(string approximateName, Entity entity)
        {
            // Try with synonyms
            var originalText = approximateName.Replace('_', ' ');
            if (entity.EntityValueSynonymsDictionary.ContainsKey(originalText))
            {
                return entity.EntityValueSynonymsDictionary[originalText];
            }
            originalText = originalText.Replace("e", "é");
            if (entity.EntityValueSynonymsDictionary.ContainsKey(originalText))
            {
                return entity.EntityValueSynonymsDictionary[originalText];
            }

            // Try with spelling correction
            IList<EntityValueProbability> entityValuesProbability = new List<EntityValueProbability>();
            foreach (var entityValue in entity.EntityValuesDictionary.Values)
            {
                var distance = StringUtils.CalcLevenshteinDistance(approximateName.ToLower(), entityValue.Name.ToLower());
                if (distance <= 2)
                {
                    entityValuesProbability.Add(new EntityValueProbability(distance, entityValue));
                }
            }
            if (entityValuesProbability.Count > 0)
            {
                return entityValuesProbability.OrderBy(p => p.distance).First().entityValue;
            }
            else
            {
                return null;
            }
        }

        public string SuggestEntityValueFromText(string approximateText)
        {
            // Try with synonyms and canonical values
            IList<TextValueProbability> entityValuesProbability = new List<TextValueProbability>();
            foreach (var canonicalValue in EntityValueSynonymsDictionary.Keys)
            {
                var distance = StringUtils.CalcLevenshteinDistance(approximateText.ToLower(), canonicalValue.ToLower());
                if (distance <= 3)
                {
                    entityValuesProbability.Add(new TextValueProbability(distance, canonicalValue));
                }
            }
            foreach (var synonym in EntityValueFromConceptDictionary.Keys)
            {
                var distance = StringUtils.CalcLevenshteinDistance(approximateText.ToLower(), synonym.ToLower());
                if (distance <= 3)
                {
                    entityValuesProbability.Add(new TextValueProbability(distance, synonym));
                }
            }
            if (entityValuesProbability.Count > 0)
            {
                return entityValuesProbability.OrderBy(p => p.distance).First().valueOrSynonym;
            }
            else
            {
                return null;
            }
        }

        private class EntityValueProbability
        {
            public EntityValueProbability(int distance, EntityValue entityValue)
            {
                this.distance = distance;
                this.entityValue = entityValue;
            }

            public int distance;
            public EntityValue entityValue;
        }

        private class TextValueProbability
        {
            public TextValueProbability(int distance, string valueOrSynonym)
            {
                this.distance = distance;
                this.valueOrSynonym = valueOrSynonym;
            }

            public int distance;
            public string valueOrSynonym;
        }       

        public EntityValue TryGetEntityValue(string canonicalValue)
        {
            if (EntityValuesDictionary == null) return null;
            else
            {
                EntityValue entityValue = null;
                if (EntityValuesDictionary.TryGetValue(canonicalValue, out entityValue))
                {
                    return entityValue;
                }
                else
                {
                    return null;
                }
            }
        }

        public EntityValue TryGetEntityValueFromConcept(string conceptCanonicalValue)
        {
            if (EntityValueFromConceptDictionary == null) return null;
            else
            {
                EntityValue entityValue = null;
                if (EntityValueFromConceptDictionary.TryGetValue(conceptCanonicalValue, out entityValue))
                {
                    return entityValue;
                }
                else
                {
                    return null;
                }
            }
        }

        public IList<DialogNode> ReferencedByDialogNodes { get; private set; }

        public void AddDialogNodeReference(DialogNode dialogNode)
        {
            if (ReferencedByDialogNodes == null) ReferencedByDialogNodes = new List<DialogNode>();
            ReferencedByDialogNodes.Add(dialogNode);
        }

        internal int LineNumber { get; set; }
    }

    public class EntityValue
    {
        public EntityValue(Entity entity, string name, string canonicalValue)
        {
            Entity = entity;
            Name = name;
            CanonicalValue = StringUtils.RemoveDiacriticsAndNonAlphanumericChars(canonicalValue);
        }

        public Entity Entity { get; private set; }
        public string Name { get; private set; }
        public string CanonicalValue { get; private set; }

        public Concept Concept { get; set; }

        public IList<string> AllowedInFederationGroups { get; private set; }
        internal void AddFederationGroup(string federationGroup)
        {
            if (AllowedInFederationGroups == null) AllowedInFederationGroups = new List<string>();
            AllowedInFederationGroups.Add(federationGroup);
        }

        public IList<DialogNode> DialogNodeReferences { get; private set; }
        internal void AddDialogNodeReference(DialogNode dialogNode)
        {
            if (DialogNodeReferences == null) DialogNodeReferences = new List<DialogNode>();
            DialogNodeReferences.Add(dialogNode);
        }

        internal int LineNumber { get; set; }
    }
    
    public class Concept
    {
        public Concept(string id, IList<string> synonyms)
        {
            // Ignore accented chars in synonyms
            for(int i = 0; i < synonyms.Count; i++)
            {
                synonyms[i] = StringUtils.RemoveDiacriticsAndNonAlphanumericChars(synonyms[i]);
            }

            Id = id;
            Key = !String.IsNullOrEmpty(id) ? id : synonyms[0];
            Synonyms = synonyms;
        }

        public string Id { get; internal set; }
        public string Key { get; internal set; }
        public string CanonicalValue { get { return Synonyms[0]; } }
        public IList<string> Synonyms { get; private set; }

        public IList<EntityValue> EntityValueReferences { get; private set; }
        internal void AddEntityValueReference(EntityValue entityValue)
        {
            if (EntityValueReferences == null) EntityValueReferences = new List<EntityValue>();
            EntityValueReferences.Add(entityValue);
        }

        internal int LineNumber { get; set; }        
        internal bool IsDuplicate { get; set; }
    }

    public class ConceptGroupWithTheSameSynonym
    {
        public ConceptGroupWithTheSameSynonym(string synonym, Concept concept)
        {
            Synonym = synonym;
            uniqueConcept = concept;
        }

        public string Synonym { get; private set; }

        private Concept uniqueConcept;
        private IList<Concept> listOfConcepts;

        public void AddConcept(Concept concept)
        {
            if(listOfConcepts == null)
            {
                listOfConcepts = new List<Concept>();
            }
            listOfConcepts.Add(concept);
        }

        public IEnumerable<Concept> Concepts
        {
            get
            {
                yield return uniqueConcept;
                if (listOfConcepts != null)
                {
                    foreach (var concept in listOfConcepts)
                    {
                        yield return concept;
                    }
                }
            }
        }
    }

    public class ConceptSubstitution
    {
        public ConceptSubstitution(string originalText, int startIndex, int stopIndex, ConceptGroupWithTheSameSynonym replacementConceptGroup, string replacementText)
        {
            OriginalText = originalText;
            StartIndex = startIndex;
            StopIndex = stopIndex;
            ReplacementConceptGroup = replacementConceptGroup;
            ReplacementText = replacementText;
        }

        public string OriginalText { get; private set; }
        public int StartIndex { get; private set; }
        public int StopIndex { get; private set; }
        public string ReplacedText { get { return OriginalText.Substring(StartIndex, StopIndex - StartIndex + 1); } }
        public string ReplacementText { get; private set; }
        public ConceptGroupWithTheSameSynonym ReplacementConceptGroup { get; private set; }

        public override string ToString()
        {
            //var beforeSubstitution = OriginalText.Substring(0, StartIndex);
            //var afterSubstitution = OriginalText.Substring(StopIndex+1, OriginalText.Length - StopIndex - 1);
            //var sentenceBefore = beforeSubstitution + ">>" + ReplacedText + "<<" + afterSubstitution;
            //var sentenceAfter = beforeSubstitution + ">>" + ReplacementText + "<<" + afterSubstitution;
            var listOfConcepts = "";
            foreach(var concept in ReplacementConceptGroup.Concepts)
            {
                listOfConcepts += " '" + concept.Key + "'";
            }
            return "concept(s)" + listOfConcepts +" : replaced \"" + ReplacedText + "\" with \"" + ReplacementText + "\"";
        }
    }

    public class EntityValueMatch
    {
        public EntityValueMatch(string textReplacedWithConcepts, int startIndex, int stopIndex, EntityValue entityValue)
        {
            TextReplacedWithConcepts = textReplacedWithConcepts;
            StartIndex = startIndex;
            StopIndex = stopIndex;
            EntityValue = entityValue;
        }

        public string TextReplacedWithConcepts { get; private set; }
        public int StartIndex { get; private set; }
        public int StopIndex { get; private set; }
        public string MatchedWord { get { return TextReplacedWithConcepts.Substring(StartIndex, StopIndex - StartIndex + 1); } }
        public EntityValue EntityValue { get; private set; }

        public override string ToString()
        {
            //var beforeSubstitution = TextReplacedWithConcepts.Substring(0, StartIndex);
            //var afterSubstitution = TextReplacedWithConcepts.Substring(StopIndex + 1, TextReplacedWithConcepts.Length - StopIndex);
            //var sentenceBefore = beforeSubstitution + ">>" + MatchedWord + "<<" + afterSubstitution;
            return "entity '" + EntityValue.Entity.Name + "' : matched \"" + MatchedWord + "\" as \'" + EntityValue.Name + "\'";
        }
    }

    public class EntityValuesMatchResult
    {
        public EntityValuesMatchResult(string originalText, 
            IList<ConceptSubstitution> conceptSubstitutions, string textReplacedWithConcepts, 
            IList<EntityValueMatch> entityValueMatches, IList<EntityValue> entityValues)
        {
            OriginalText = originalText;
            ConceptSubstitutions = conceptSubstitutions;
            TextReplacedWithConcepts = textReplacedWithConcepts;
            EntityValueMatches = entityValueMatches;
            EntityValues = entityValues;
        }

        public string OriginalText { get; private set; }

        public IList<ConceptSubstitution> ConceptSubstitutions { get; private set; }
        public string TextReplacedWithConcepts { get; private set; }

        public IList<EntityValueMatch> EntityValueMatches { get; private set; }
        public IList<EntityValue> EntityValues { get; private set; }
    }
}
