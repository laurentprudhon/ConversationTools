using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace dialogtool
{
    public class Dialog : IMessageCollector
    {
        // Default config parameters for CM-CIC insurance & savings dialogs
        static string insuranceSpecificEntity = "GUARANTEE_ENTITY";

        public Dialog(string filePath)
        {
            FilePath = filePath;
            Errors = new List<string>();

            Variables = new Dictionary<string, DialogVariable>();
            Constants = new Dictionary<string, Constant>();
            Entities = new Dictionary<string, Entity>();
            Concepts = new Dictionary<string, Concept>();
            ConceptsSynonyms = new Dictionary<string, ConceptGroupWithTheSameSynonym>(StringComparer.InvariantCultureIgnoreCase);

            Intents = new Dictionary<string, MatchIntentAndEntities>();
            DialogNodesWithId = new Dictionary<string, DialogNode>();
        }

        public string FilePath { get; private set; }
        public MappingUriGenerator.MappingUriConfig MappingUriConfig { get; private set; }

        public IDictionary<string, MatchIntentAndEntities> Intents { get; private set; }
        public IDictionary<string, Entity> Entities { get; private set; }
        public IDictionary<string, Concept> Concepts { get; private set; }
        public IDictionary<string, ConceptGroupWithTheSameSynonym> ConceptsSynonyms { get; private set; }
        internal Regex ConceptsRegex { get; set; }

        public IDictionary<string, Constant> Constants { get; private set; }
        public IDictionary<string, DialogVariable> Variables { get; private set; }
        public IDictionary<string, IDictionary<string, IList<string>>> ArraysOfAllowedValuesByEntityNameAndFederation { get; internal set; }

        public IDictionary<string, DialogNode> DialogNodesWithId { get; private set; }
        public string StartOfDialogNodeId { get; set; }
        public string[] FatHeadAnswerNodeIds { get; set; }
        public string LongTailAnswerNodeId { get; set; }

        internal void DetectMappingURIsConfig()
        {
            if (Entities.ContainsKey(insuranceSpecificEntity))
            {
                MappingUriConfig = MappingUriGenerator.MappingUriConfig.Insurance;
            }
            else
            {
                MappingUriConfig = MappingUriGenerator.MappingUriConfig.Savings;
            }
        }

        public void ResolveAndCheckReferences()
        {
            // Try to resolve all goto references
            foreach (var childNode in Intents.Values.SelectMany(intent => intent.ChildrenNodes))
            {
                ResolveGotoReferences(childNode);
            }

            // Check if all constants were referenced at least once in dialog
            foreach (var constant in Constants.Values)
            {
                if (constant.DialogNodeReferences == null)
                {
                    LogMessage(constant.LineNumber, MessageType.NeverUsed, "Constant \"" + constant.Name + "\" isn't referenced by any dialog node : may be obsolete");
                }
            }
            // Check if all variables were referenced at least once in dialog
            foreach (var variable in Variables.Values)
            {
                if (variable.DialogNodeReferences == null)
                {
                    LogMessage(variable.LineNumber, MessageType.NeverUsed, "Variable \"" + variable.Name + "\" isn't referenced by any dialog node : may be obsolete");
                }
            }
            // Check if all concepts were referenced at least once by entity values
            foreach (var concept in Concepts.Values)
            {
                if (concept.EntityValueReferences == null && ! concept.IsDuplicate)
                {
                    LogMessage(concept.LineNumber, MessageType.NeverUsed, "Concept \"" + (concept.Id != null ? concept.Id : concept.CanonicalValue) + "\" isn't referenced by any entity value : may be osbolete");
                }
            }
            // Check if all entity values were referenced at least once in dialog
            foreach (var entity in Entities.Values)
            {
                foreach (var entityValue in entity.Values)
                {
                    if (entityValue.DialogNodeReferences == null)
                    {
                        LogMessage(entityValue.LineNumber, MessageType.NeverUsed, "Entity value " + entity.Name + " >  \"" + entityValue.Name + "\" isn't referenced by any dialog node : may be obsolete");
                    }
                }
            }
        }

        private DialogNode previousNode;

        private void ResolveGotoReferences(DialogNode dialogNode)
        {
            if(previousNode != null && previousNode.Type == DialogNodeType.GotoNode)
            {
                var previousGotoNode = (GotoNode)previousNode;
                if(previousGotoNode.TargetNode == dialogNode)
                {
                    previousGotoNode.Type = DialogNodeType.GotoNext;
                }
            }
            if (dialogNode.Type == DialogNodeType.GotoNode)
            {
                var gotoNode = (GotoNode)dialogNode;
                if (!String.IsNullOrEmpty(gotoNode.TargetNodeId))
                {
                    if (DialogNodesWithId.ContainsKey(gotoNode.TargetNodeId))
                    {
                        var targetNode = DialogNodesWithId[gotoNode.TargetNodeId];
                        gotoNode.TargetNode = targetNode;
                        targetNode.AddDialogNodeReference(gotoNode);
                    }
                    else
                    {
                        LogMessage(gotoNode.LineNumber, MessageType.InvalidReference, "Goto node with invalid node reference : \"" + gotoNode.TargetNodeId + "\" (target node may be offline) => dead end");
                    }
                }
            }
            previousNode = dialogNode;
            if (dialogNode.ChildrenNodes != null)
            {
                foreach (var childNode in dialogNode.ChildrenNodes)
                {
                    ResolveGotoReferences(childNode);
                }
            }
        }

        public IList<string> Errors { get; private set; }

        public void LogMessage(int lineNumber, MessageType type, string message)
        {
            Errors.Add(lineNumber.ToString("00000") + ";" + type + ";" + message);
        }

        internal void AddVariable(DialogVariable variable)
        {
            Variables.Add(variable.Name, variable);
        }


        internal void AddConstant(Constant constant)
        {
            Constants.Add(constant.Name, constant);
        }

        internal void AddConcept(Concept concept)
        {
            // Check for duplicate synonyms in a single concept
            ISet<string> thisConceptSynonyms = new HashSet<string>();
            int i = 1;
            foreach (var synonym in concept.Synonyms)
            {
                i++;
                if (thisConceptSynonyms.Contains(synonym))
                {
                    LogMessage(concept.LineNumber + i, MessageType.DuplicateSynonym, "Duplicate synonym : \"" + synonym + "\" inside the same concept \"" + concept.Key + "\"");
                }
                else
                {
                    thisConceptSynonyms.Add(synonym);
                }
            }

            // Add concept in dictionary of concepts
            Concept conceptInDictionary = null;
            if (!String.IsNullOrEmpty(concept.Id))
            {
                if (Concepts.ContainsKey(concept.Id))
                {
                    conceptInDictionary = Concepts[concept.Id];
                    // Mark second concept as duplicate
                    concept.IsDuplicate = true;
                    // Merge second concept synonyms
                    foreach(var synonym in concept.Synonyms)
                    {
                        if(!conceptInDictionary.Synonyms.Contains(synonym))
                        {
                            conceptInDictionary.Synonyms.Add(synonym);
                        }
                    }
                    // Log error message
                    LogMessage(concept.LineNumber, MessageType.DuplicateConcept, "Duplicate concept nodes found for id \"" + concept.Id + "\" : line " + concept.LineNumber + " and line " + conceptInDictionary.LineNumber);
                }
                else if (Concepts.ContainsKey(concept.CanonicalValue))
                {
                    conceptInDictionary = Concepts[concept.CanonicalValue];
                    // Mark second concept as duplicate
                    concept.IsDuplicate = true;
                    // Merge second concept synonyms
                    foreach (var synonym in concept.Synonyms)
                    {
                        if (!conceptInDictionary.Synonyms.Contains(synonym))
                        {
                            conceptInDictionary.Synonyms.Add(synonym);
                        }
                    }
                    // Merge second concept Id
                    if (String.IsNullOrEmpty(conceptInDictionary.Id))
                    {
                        conceptInDictionary.Id = concept.Id;
                        conceptInDictionary.Key = concept.Id;
                        Concepts.Add(conceptInDictionary.Id, conceptInDictionary);
                    }
                    // Log error message
                    LogMessage(concept.LineNumber, MessageType.DuplicateConcept, "Duplicate concept nodes found for canonical value \"" + concept.CanonicalValue + "\" : line " + concept.LineNumber + " and line " + conceptInDictionary.LineNumber + " => you should probably merge them");
                }
                else
                {
                    conceptInDictionary = concept;
                    Concepts.Add(concept.Id, concept);
                    Concepts.Add(concept.CanonicalValue, concept);
                }
            }
            else
            {
                if (Concepts.ContainsKey(concept.CanonicalValue))
                {
                    conceptInDictionary = Concepts[concept.CanonicalValue];
                    // Mark second concept as duplicate
                    concept.IsDuplicate = true;
                    // Merge second concept synonyms
                    foreach (var synonym in concept.Synonyms)
                    {
                        if (!conceptInDictionary.Synonyms.Contains(synonym))
                        {
                            conceptInDictionary.Synonyms.Add(synonym);
                        }
                    }
                    // Log error message
                    LogMessage(concept.LineNumber, MessageType.DuplicateConcept, "Duplicate concept nodes found for canonical value \"" + concept.CanonicalValue + "\" : line " + concept.LineNumber + " and line " + conceptInDictionary.LineNumber + " => you should probably merge them");
                }
                else
                {
                    conceptInDictionary = concept;
                    Concepts.Add(concept.CanonicalValue, concept);
                }
            }
                        
            // Add concept synonyms in dictionary of synonyms
            foreach (var synonym in thisConceptSynonyms)
            {
                if (ConceptsSynonyms.ContainsKey(synonym))
                {
                    var conceptGroupWithTheSameSynonym = ConceptsSynonyms[synonym];
                    conceptGroupWithTheSameSynonym.AddConcept(conceptInDictionary);
                }
                else
                {
                    ConceptsSynonyms[synonym] = new ConceptGroupWithTheSameSynonym(synonym, conceptInDictionary);
                }
            }
        }

        internal void OnAllConceptsAdded()
        {
            // Compile the global concepts Regex
            StringBuilder regexBuilder = new StringBuilder();
            regexBuilder.Append("\\b(");           
            bool isFirstSynonym = true;
            foreach (string synonym in ConceptsSynonyms.Keys.OrderByDescending(s => s.Length))
            {
                if (!String.IsNullOrEmpty(synonym))
                {
                    if (isFirstSynonym)
                    {
                        isFirstSynonym = false;
                    }
                    else
                    {
                        regexBuilder.Append("|");
                    }
                    regexBuilder.Append(Regex.Escape(synonym));
                }
            }
            regexBuilder.Append(")\\b");
            ConceptsRegex = new Regex(regexBuilder.ToString(), RegexOptions.IgnoreCase);
        }

        internal void LinkEntityValueToConcept(EntityValue entityValue, string conceptId)
        {
            if (conceptId != null)
            {
                if (Concepts.ContainsKey(conceptId))
                {
                    var concept = Concepts[conceptId];
                    entityValue.Concept = concept;
                    concept.AddEntityValueReference(entityValue);
                }
                else
                {
                    LogMessage(entityValue.LineNumber, MessageType.InvalidReference, "Entity value " + entityValue.Entity.Name + " > \"" + entityValue.Name + "\" => invalid concept reference : " + conceptId + " (a conflicting concept with the same canonical value but a different id may have been defined before)");
                }
            }
            else if (Concepts.ContainsKey(entityValue.CanonicalValue))
            {
                var concept = Concepts[entityValue.CanonicalValue];
                entityValue.Concept = concept;
                concept.AddEntityValueReference(entityValue);
            }
        }

        internal void AddEntity(Entity entity)
        {
            entity.OnAllEntityValuesAdded(this);
            Entities.Add(entity.Name, entity);
        }

        internal void OnAllEntitiesAdded()
        {
            // Check if all entity values are mutually exclusive
            IDictionary<string, EntityValue> crossEntityValuesDictionary = new Dictionary<string, EntityValue>();
            foreach (var entity in Entities.Values)
            {
                foreach (var text in entity.EntityValuesDictionary.Keys)
                {
                    var entityValue = entity.EntityValuesDictionary[text];
                    if (crossEntityValuesDictionary.ContainsKey(text))
                    {
                        var conflictingEntityValue = crossEntityValuesDictionary[text];
                        if (entityValue.Entity != conflictingEntityValue.Entity)
                        {
                            LogMessage(entityValue.LineNumber, MessageType.Info, "[Info - this isn't a problem] The same entity value : \"" + text + "\" is used by two distinct entities : " + conflictingEntityValue.Entity.Name + " > \"" + conflictingEntityValue.Name + "\" (line " + conflictingEntityValue.LineNumber + "), and " + entityValue.Entity.Name + " > \"" + entityValue.Name + "\" (line " + entityValue.LineNumber + ")");
                        }
                    }
                    else
                    {
                        crossEntityValuesDictionary.Add(text, entityValue);
                    }
                }
            }
            // Check if all entity concepts are mutually exclusive
            IDictionary<string, EntityValue> crossEntityValueConceptsDictionary = new Dictionary<string, EntityValue>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var entity in Entities.Values)
            {
                foreach (var text in entity.EntityValueFromConceptDictionary.Keys)
                {
                    var entityValue = entity.EntityValueFromConceptDictionary[text];
                    if (crossEntityValueConceptsDictionary.ContainsKey(text))
                    {
                        var conflictingEntityValue = crossEntityValueConceptsDictionary[text];
                        if (entityValue.Entity != conflictingEntityValue.Entity)
                        {
                            LogMessage(entityValue.LineNumber, MessageType.Info, "[Info - this isn't a problem] The same concept \"" + text + "\"  is used by two distinct entities : " + conflictingEntityValue.Entity.Name + " > \"" + conflictingEntityValue.Name + "\" (line " + conflictingEntityValue.LineNumber + "), and " + entityValue.Entity.Name + " > \"" + entityValue.Name + "\" (line " + entityValue.LineNumber + ")");
                        }
                    }
                    else
                    {
                        crossEntityValueConceptsDictionary.Add(text, entityValue);
                    }
                }
            }
            // Compute list of federation groups allowed for each entity value
            if (ArraysOfAllowedValuesByEntityNameAndFederation != null)
            {
                foreach (var entity in Entities.Values)
                {
                    // Arrays of allowed values by federation
                    IDictionary<string, IList<string>> valuesAllowedByFederation = null;
                    ArraysOfAllowedValuesByEntityNameAndFederation.TryGetValue(entity.Name, out valuesAllowedByFederation);
                    if (valuesAllowedByFederation != null)
                    {
                        foreach (var entityValue in entity.EntityValueNamesDictionary.Values)
                        {
                            foreach (var federationGroup in valuesAllowedByFederation.Keys)
                            {
                                var valuesForThisFederationGroup = valuesAllowedByFederation[federationGroup];
                                if (valuesForThisFederationGroup.Contains(entityValue.Name))
                                {
                                    entityValue.AddFederationGroup(federationGroup);
                                }
                            }
                            if(entityValue.AllowedInFederationGroups == null)
                            {
                                LogMessage(entityValue.LineNumber, MessageType.Info, "Entity value : " + entity.Name + " > " + entityValue.Name + " is not allowed in any federation group");
                            }
                        }
                    }

                }
            }
        }            

        internal void RegisterDialogNode(DialogNode dialogNode)
        {
            if (DialogNodesWithId.ContainsKey(dialogNode.Id))
            {
                var otherNode = DialogNodesWithId[dialogNode.Id];
                LogMessage(dialogNode.LineNumber, MessageType.DuplicateKey, "Two distinct dialog nodes share the same id \"" + dialogNode.Id + "\" : line " + otherNode.LineNumber + " and line " + dialogNode.LineNumber);
            }
            else
            {
                DialogNodesWithId.Add(dialogNode.Id, dialogNode);
            }
        }

        internal void AddIntent(MatchIntentAndEntities intent, DialogVariablesSimulator dialogVariables)
        {
            if (!Intents.ContainsKey(intent.Name))
            {
                Intents.Add(intent.Name, intent);
                dialogVariables.AddMatchIntentAndEntities(intent);
            }
            else
            {
                var otherIntent = Intents[intent.Name];
                LogMessage(intent.LineNumber, MessageType.DuplicateKey, "Two intent matching nodes found for intent \"" + intent.Name + "\" : line " + intent.LineNumber + " and line " + otherIntent.LineNumber);
            }
        }

        internal void LinkVariableAssignmentToVariable(DialogNode dialogNode, DialogVariableAssignment variableAssignment)
        {
            variableAssignment.Variable = Variables[variableAssignment.VariableName];
            variableAssignment.Variable.AddDialogNodeReference(dialogNode, VariableReferenceType.Write);
            if(variableAssignment.Operator == DialogVariableOperator.CopyValueFromVariable)
            {
                var refVariable = Variables[variableAssignment.Value];
                refVariable.AddDialogNodeReference(dialogNode, VariableReferenceType.Read);
            }
        }

        internal void LinkEntityMatchToEntityAndDialogVariables(DialogNode dialogNode, EntityMatch entityMatch)
        {
            entityMatch.Entity = Entities[entityMatch.EntityName];
            entityMatch.Entity.AddDialogNodeReference(dialogNode);

            if (entityMatch.EntityVariableName1 != null)
            {
                entityMatch.EntityVariable1 = Variables[entityMatch.EntityVariableName1];
                entityMatch.EntityVariable1.AddDialogNodeReference(dialogNode, VariableReferenceType.Write);
            }
            if (entityMatch.EntityVariableName2 != null)
            {
                entityMatch.EntityVariable2 = Variables[entityMatch.EntityVariableName2];
                entityMatch.EntityVariable2.AddDialogNodeReference(dialogNode, VariableReferenceType.Write);
            }
        }

        internal Constant TryGetConstant(int lineNumber, string constantName)
        {
            Constant constant = null;
            if (Constants.ContainsKey(constantName))
            {
                constant = Constants[constantName];
            }
            else
            {
                LogMessage(lineNumber, MessageType.InvalidReference, "Prompt item references undefined constant name " + constantName);
            }
            return constant;
        }

        internal EntityValuesMatchResult MatchEntityValueInOptionText(DialogNode dialogNode, Entity entity, string optionText)
        {
            return EntityValuesMatcher.MatchEntityValues(new Entity[] { entity }, optionText, ConceptsSynonyms, ConceptsRegex);
        }

        internal void LinkDialogVariableConditionToDialogVariableAndEntityValue(DialogNode dialogNode, DialogVariableCondition variableCondition, DialogVariablesSimulator dialogVariables)
        {
            if (String.IsNullOrEmpty(variableCondition.VariableName))
            {
                return;
            }

            DialogVariable variable = null;
            EntityValue entityValue = null;
            if (!Variables.ContainsKey(variableCondition.VariableName))
            {
                LogMessage(dialogNode.LineNumber, MessageType.InvalidReference, "Variable condition references undefined variable name : \"" + variableCondition.VariableName + "\"");
            }
            else
            {
                variable = Variables[variableCondition.VariableName];
                variable.AddDialogNodeReference(dialogNode, VariableReferenceType.Read);

                if (variableCondition.Comparison != ConditionComparison.HasValue && !String.IsNullOrEmpty(variableCondition.Value))
                {
                    var entity = dialogVariables.TryGetEntityFromVariable(variable);
                    if (entity != null)
                    {
                        entityValue = entity.TryGetEntityValueFromName(variableCondition.Value);
                        if (entityValue == null)
                        {
                            var message = "Variable condition references undefined entity value name \"" + variableCondition.Value + "\" for entity " + entity.Name;
                            var suggestedEntityValue = entity.SuggestEntityValueFromName(variableCondition.Value, Entities.Values);
                            if (suggestedEntityValue != null)
                            {
                                if (suggestedEntityValue.Entity == entity)
                                {
                                    message += ", you may want to replace this name with \"" + suggestedEntityValue.Name + "\" (defined line " + suggestedEntityValue.LineNumber + ")";
                                }
                                else
                                {
                                    message += ", you may want to move this condition under another entity \"" + suggestedEntityValue.Entity.Name + "\" with value \"" + suggestedEntityValue.Name + "\" (defined line " + suggestedEntityValue.LineNumber + ")";
                                }
                            }
                            LogMessage(dialogNode.LineNumber, MessageType.InvalidReference, message);
                        }
                        else
                        {
                            entityValue.AddDialogNodeReference(dialogNode);
                        }
                    }
                }
            }
            variableCondition.SetVariableAndEntityValue(variable, entityValue);
        }

        public IDictionary<DialogNodeType,int> ComputeNodesStatistics()
        {
            var nodeTypeCounts = new Dictionary<DialogNodeType, int>();
            foreach (var value in Enum.GetValues(typeof(DialogNodeType)))
            {
                var nodeType = (DialogNodeType)value;
                nodeTypeCounts.Add(nodeType, 0);
            }
            foreach (var intentNode in Intents.Values)
            {
                ComputeNodesStatistics(intentNode, nodeTypeCounts);
            }
            return nodeTypeCounts;
        }

        private static void ComputeNodesStatistics(DialogNode parentNode, IDictionary<DialogNodeType, int> nodeTypeCounts)
        {
            nodeTypeCounts[parentNode.Type]++;
            if(parentNode.ChildrenNodes != null)
            {
                foreach(var childNode in parentNode.ChildrenNodes)
                {
                    ComputeNodesStatistics(childNode, nodeTypeCounts);
                }
            }
        }
    }

    public interface IMessageCollector
    {
        void LogMessage(int lineNumber, MessageType type, string message);
    }

    public enum MessageType
    {
        Info,
        DuplicateKey,
        DuplicateConcept,
        DuplicateSynonym,
        IncorrectPattern,
        InvalidReference,
        NeverUsed
    }
    
    public class Constant
    {
        public Constant(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }
        public string Value { get; private set; }

        public IList<DialogNode> DialogNodeReferences { get; private set; }
        internal void AddDialogNodeReference(DialogNode dialogNode)
        {
            if (DialogNodeReferences == null) DialogNodeReferences = new List<DialogNode>();
            DialogNodeReferences.Add(dialogNode);
        }

        internal int LineNumber { get; set; }
    }
}
