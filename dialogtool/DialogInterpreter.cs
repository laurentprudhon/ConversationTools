﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dialogtool
{
    public static class DialogInterpreter
    {
        public static DialogExecutionResult AnalyzeInitialQuestion(Dialog dialog, string questionId, string questionText, string intentName)
        {
            var result = new DialogExecutionResult(questionId, questionText, intentName);

            MatchIntentAndEntities intent = null;
            if (!dialog.Intents.TryGetValue(intentName, out intent))
            {
                intent = dialog.Intents.Values.Where(i => i.Name.EndsWith(intentName)).FirstOrDefault();
            }

            if(intent != null)
            { 
                ExecuteUserInputNode(dialog, intent, questionText, result);
            }
            else
            {
                result.LogMessage("Intent name " + intentName + " undefined in dialog file " + dialog.FilePath);
            }

            return result;
        }

        public static DialogExecutionResult ExecuteUserInputNode(Dialog dialog, DialogNode dialogNode, string userInputText, DialogExecutionResult result)
        {
            IEnumerable<EntityMatch> entityMatches = null;
            if(dialogNode is MatchIntentAndEntities)
            {
                var intent = (MatchIntentAndEntities)dialogNode;
                entityMatches = intent.EntityMatches;
            }
            else if(dialogNode is DisambiguationQuestion)
            {
                var question = (DisambiguationQuestion)dialogNode;
                if (question.EntityMatch != null)
                {
                    entityMatches = new EntityMatch[] { question.EntityMatch };
                }
                result.AddUserInput(userInputText);
            }
            else
            {
                throw new ArgumentException("Dialog node must be of type MatchIntentAndEntities or DisambiguationQuestion");
            }

            EntityValuesMatchResult matchResult = null;
            if (entityMatches != null)
            {
                // Try to match entity values in the questions
                var entities = entityMatches.Select(entityMatch => entityMatch.Entity);
                matchResult = EntityValuesMatcher.MatchEntityValues(entities, userInputText, dialog.ConceptsSynonyms, dialog.ConceptsRegex);
                
                // Store the matched entity values in their assigned variables
                foreach (var entityValuesGroup in matchResult.EntityValues.GroupBy(ev => ev.Entity))
                {
                    var entityMatch = entityMatches.Where(em => em.Entity == entityValuesGroup.Key).First();
                    int matchIndex = 0;
                    foreach (var entityValue in entityValuesGroup)
                    {
                        if (matchIndex == 0 && entityMatch.EntityVariableName1 != null)
                        {
                            result.VariablesValues[entityMatch.EntityVariableName1] = entityValue.Name;
                        }
                        else if (matchIndex == 1 && entityMatch.EntityVariableName2 != null)
                        {
                            result.VariablesValues[entityMatch.EntityVariableName2] = entityValue.Name;
                        }
                        matchIndex++;
                    }
                }
                ExecuteVariableAssignments(dialogNode, result.VariablesValues);
            }

            // Store the result of this execution
            var nodeExecution = new MatchEntitiesNodeExecution(dialogNode, matchResult);
            if(result.ExecutionResult != null && result.ExecutionResult.DialogNode == dialogNode)
            {
                result.DialogNodesExecutionPath.Remove(result.ExecutionResult);
            }
            result.AddDialogNodeExecution(nodeExecution);

            // Traverse the children nodes
            SelectChildNode(dialog, result.VariablesValues, dialogNode, null, result);

            return result;
        }

        private static void ExecuteVariableAssignments(DialogNode dialogNode, IDictionary<string, string> variablesValues)
        {
            if (dialogNode.VariableAssignments != null)
            {
                foreach (var assignment in dialogNode.VariableAssignments)
                {
                    if (assignment.Operator == DialogVariableOperator.CopyValueFromVariable)
                    {
                        if(variablesValues.ContainsKey(assignment.Value))
                        {
                            variablesValues[assignment.VariableName] = variablesValues[assignment.Value];
                        }
                        else
                        {
                            variablesValues.Remove(assignment.VariableName);
                        }
                    }
                    else
                    {
                        variablesValues[assignment.VariableName] = assignment.Value;
                    }
                }
            }
        }

        private static void SelectChildNode(Dialog dialog, IDictionary<string, string> variablesValues, DialogNode parentNode, DialogNode firstChildNode, DialogExecutionResult result)
        {
            bool firstChildNodeFound = firstChildNode == null;
            foreach (var childNode in parentNode.ChildrenNodes)
            {
                if (!firstChildNodeFound)
                {
                    firstChildNodeFound = childNode == firstChildNode;
                    if (!firstChildNodeFound) continue;
                }

                switch (childNode.Type)
                {
                    case DialogNodeType.SwitchOnEntityVariables:
                        var switchNode = (SwitchOnEntityVariables)childNode;
                        string varValue = null;
                        if (variablesValues.TryGetValue(switchNode.EntityMatch.EntityVariableName1, out varValue))
                        {
                            if (!String.IsNullOrEmpty(varValue))
                            {
                                ExecuteConditionalNode(dialog, variablesValues, childNode, result);
                                if (result.ExecutionResult.DialogNode.Type != DialogNodeType.SwitchOnEntityVariables)
                                {
                                    return;
                                }
                            }
                        }
                        break;
                    case DialogNodeType.SwitchLoopOnce:
                        var switchLoopNode = (SwitchLoopOnce)childNode;
                        switchNode = (SwitchOnEntityVariables)switchLoopNode.ParentNode;
                        // loop only if variable2 not null
                        string var2Value = null;
                        if (variablesValues.TryGetValue(switchNode.EntityMatch.EntityVariableName2, out var2Value))
                        {
                            if (!String.IsNullOrEmpty(var2Value))
                            {
                                // Set variable1 to variable 2 and reset variable 2
                                variablesValues[switchNode.EntityMatch.EntityVariableName1] = var2Value;
                                variablesValues[switchNode.EntityMatch.EntityVariableName2] = null;

                                // Store the result of this execution
                                var nodeExecution = new DialogNodeExecution(switchLoopNode);
                                result.AddDialogNodeExecution(nodeExecution);

                                // Adjust variables values
                                ExecuteVariableAssignments(switchLoopNode, variablesValues);

                                // Loop back to switch node
                                SelectChildNode(dialog, variablesValues, switchNode.ParentNode, switchNode, result);
                                return;
                            }
                        }
                        break;
                    case DialogNodeType.DialogVariableConditions:
                        var conditionsNode = (DialogVariableConditions)childNode;
                        bool conditionsTest = conditionsNode.Operator == ConditionOperator.And ? true : false;
                        foreach (var condition in conditionsNode.VariableConditions)
                        {
                            varValue = null;
                            variablesValues.TryGetValue(condition.VariableName, out varValue);
                            bool conditionTest = false;
                            if (condition.Comparison == ConditionComparison.HasValue)
                            {
                                if (!String.IsNullOrEmpty(varValue))
                                {
                                    conditionTest = true;
                                }
                            }
                            else if (condition.Comparison == ConditionComparison.Equals)
                            {
                                if ((String.IsNullOrEmpty(condition.Value) && String.IsNullOrEmpty(varValue)) || (condition.Value != null && condition.Value == varValue))
                                {
                                    conditionTest = true;
                                }
                            }
                            if (conditionsNode.Operator == ConditionOperator.And)
                            {
                                conditionsTest &= conditionTest;
                            }
                            else
                            {
                                conditionsTest |= conditionTest;
                            }
                        }
                        if (conditionsTest)
                        {
                            ExecuteConditionalNode(dialog, variablesValues, childNode, result);
                            return;
                        }
                        break;
                    case DialogNodeType.DirectAnswer:
                    case DialogNodeType.DisambiguationQuestion:
                    case DialogNodeType.RedirectToLongTail:
                        ExecutePromptNode(dialog, variablesValues, childNode, result);
                        return;
                    case DialogNodeType.FatHeadAnswers:
                        ExecuteFatHeadNode(dialog, variablesValues, (FatHeadAnswers)childNode, result);
                        return;
                    case DialogNodeType.GotoNext:
                    case DialogNodeType.GotoNode:
                        NavigateToNextNode(dialog, variablesValues, (GotoNode)childNode, result);
                        return;
                }
            }
        }

        private static void ExecuteConditionalNode(Dialog dialog, IDictionary<string, string> variablesValues, DialogNode conditionalNode, DialogExecutionResult result)
        {
            // Store the result of this execution
            var nodeExecution = new DialogNodeExecution(conditionalNode);
            result.AddDialogNodeExecution(nodeExecution);

            // Adjust variables values
            ExecuteVariableAssignments(conditionalNode, variablesValues);

            // Traverse the children nodes
            SelectChildNode(dialog, variablesValues, conditionalNode, null, result);
        }

        private static void ExecutePromptNode(Dialog dialog, IDictionary<string, string> variablesValues, DialogNode promptNode, DialogExecutionResult result)
        {
            // Store the result of this execution
            var nodeExecution = new DialogNodeExecution(promptNode);
            result.AddDialogNodeExecution(nodeExecution);

            // Stop the traversal at the first prompt to the user
            // (what comes later is unpredictable)
        }

        private static void ExecuteFatHeadNode(Dialog dialog, IDictionary<string, string> variablesValues, FatHeadAnswers fatHeadAnswerNode, DialogExecutionResult result)
        {
            // Adjust variables values
            ExecuteVariableAssignments(fatHeadAnswerNode, variablesValues);

            // Build the mapping URI
            bool redirectToLongTail;
            bool directAnswserValueNotSupportedInFederation;
            var mappingUri = MappingUriGenerator.ComputeMappingURI(variablesValues, dialog.MappingUriConfig, out redirectToLongTail, out directAnswserValueNotSupportedInFederation);

            // Store the result of this execution
            DialogNodeExecution nodeExecution = null;
            if (redirectToLongTail || directAnswserValueNotSupportedInFederation || String.IsNullOrEmpty(mappingUri))
            {
                nodeExecution = new DialogNodeExecution(fatHeadAnswerNode);
            }
            else
            {
                nodeExecution = new FatHeadAnswerNodeExecution(fatHeadAnswerNode, mappingUri);
            }
            result.AddDialogNodeExecution(nodeExecution);
        }

        private static void NavigateToNextNode(Dialog dialog, IDictionary<string, string> variablesValues, GotoNode gotoNode, DialogExecutionResult result)
        {
            // Adjust variables values
            ExecuteVariableAssignments(gotoNode, variablesValues);

            // Store the result of this execution
            var nodeExecution = new DialogNodeExecution(gotoNode);
            result.AddDialogNodeExecution(nodeExecution);

            // Error message if no target node found
            if(gotoNode.TargetNode == null)
            {
                result.LogMessage("Goto node is a dead end : the reference to target node id '" + gotoNode.TargetNodeId + "' could not be resolved");
            }
            else
            {
                // Contine with the target node and its siblings
                SelectChildNode(dialog, variablesValues, gotoNode.TargetNode.ParentNode, gotoNode.TargetNode, result);
            }
        }
    }

    public class DialogExecutionResult
    {
        public DialogExecutionResult(string questionId, string questionText, string intentName)
        {
            QuestionId = questionId;
            QuestionText = questionText;
            IntentName = intentName;

            DialogNodesExecutionPath = new List<DialogNodeExecution>();
            VariablesValues = new Dictionary<string, string>();
        }

        public string QuestionId { get; private set; }
        public string QuestionText { get; private set; }
        public string IntentName { get; private set; }

        public void AddUserInput(string userInputText)
        {
            if (UserInputs == null) UserInputs = new List<string>();
            UserInputs.Add(userInputText);
        }
        public IList<string> UserInputs { get; private set; }
        
        public IList<DialogNodeExecution> DialogNodesExecutionPath { get; private set; }
        public void AddDialogNodeExecution(DialogNodeExecution dialogNodeExecution)
        {
            DialogNodesExecutionPath.Add(dialogNodeExecution);
        }

        public DialogNodeExecution ExecutionResult
        {
            get { return DialogNodesExecutionPath.Count > 0 ? DialogNodesExecutionPath[DialogNodesExecutionPath.Count - 1] : null;  }
        }
        public string ResultString
        {
            get {
                if (ExecutionResult != null)
                {
                    return ExecutionResult.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public IDictionary<string, string> VariablesValues { get; private set; }

        public IList<string> Messages { get; private set; }
        public void LogMessage(string message)
        {
            if (Messages == null) Messages = new List<string>();
            Messages.Add(message);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (var nodeExecution in DialogNodesExecutionPath)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(" >> ");
                }
                sb.Append('[');
                sb.Append(nodeExecution.ToString());
                sb.Append(']');
            }
            return sb.ToString();
        }

        internal bool ReturnsSameResultAs(DialogExecutionResult oldResult)
        {
            if (oldResult.DialogNodesExecutionPath.Count >= 1 && this.DialogNodesExecutionPath.Count >= 1)
            {
                return oldResult.ResultString == this.ResultString;
            }
            else
            {
                return oldResult.DialogNodesExecutionPath.Count == 0 && this.DialogNodesExecutionPath.Count == 0;
            }
        }
    }

    public class DialogNodeExecution
    {
        public DialogNodeExecution(DialogNode dialogNode)
        {
            DialogNode = dialogNode;
        }

        public DialogNode DialogNode { get; private set; }

        public override string ToString()
        {
            return DialogNode.ToString();
        }

        public IList<DialogNodeExecution> Children { get; private set; }
        public void AddChild(DialogNodeExecution child)
        {
            if (Children == null) Children = new List<DialogNodeExecution>();
            Children.Add(child);
        }
    }

    public class MatchEntitiesNodeExecution : DialogNodeExecution
    {
        public MatchEntitiesNodeExecution(DialogNode dialogNode, EntityValuesMatchResult entityValuesMatchResult) : base(dialogNode)
        {
            EntityValuesMatchResult = entityValuesMatchResult;
        }

        public EntityValuesMatchResult EntityValuesMatchResult { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            if (EntityValuesMatchResult != null)
            {
                if (EntityValuesMatchResult.ConceptSubstitutions != null && EntityValuesMatchResult.ConceptSubstitutions.Count > 0)
                {
                    sb.Append("\n  > concepts substitutions");
                    foreach (var substitution in EntityValuesMatchResult.ConceptSubstitutions)
                    {
                        sb.Append("\n   - ");
                        sb.Append(substitution.ToString());
                    }
                }
                if (EntityValuesMatchResult.EntityValueMatches != null && EntityValuesMatchResult.EntityValueMatches.Count > 0)
                {
                    sb.Append("\n  > entity values");
                    foreach (var entityValueMatch in EntityValuesMatchResult.EntityValueMatches)
                    {
                        sb.Append("\n   - ");
                        sb.Append(entityValueMatch.ToString());
                    }
                }
            }
            else
            {
                sb.Append("\n  > ERROR pattern NOT YET SUPPORTED : disambiguation options not based on entity values");
            }
            return sb.ToString(); ;
        }
    }

    public class FatHeadAnswerNodeExecution : DialogNodeExecution
    {
        public FatHeadAnswerNodeExecution(DialogNode dialogNode, string mappingUri) : base(dialogNode)
        {
            MappingURI = mappingUri;
        }

        public string MappingURI { get; private set; }

        public override string ToString()
        {
            return "FatHeadAnswer:" + MappingURI;
        }

        internal static string CompareMappingURIs(string newURI, string oldURI)
        {
            var newValues = SplitMappingURI(newURI);
            var oldValues = SplitMappingURI(oldURI);

            StringBuilder diff = new StringBuilder();
            foreach (var newKey in newValues.Keys)
            {
                if(!oldValues.ContainsKey(newKey))
                {
                    diff.Append("added " + newKey + "=" + newValues[newKey]+ " ");
                }
                else if(oldValues[newKey] != newValues[newKey])
                {
                    diff.Append("changed " + newKey + " : " + oldValues[newKey] + " => " + newValues[newKey] + " ");
                }
            }
            foreach (var oldKey in oldValues.Keys)
            {
                if (!newValues.ContainsKey(oldKey))
                {
                    diff.Append("removed " + oldKey + " ");
                }
            }
            return diff.ToString();
        }

        private static IDictionary<string, string> SplitMappingURI(string uri)
        {
            var result = new Dictionary<string, string>();
            if(!String.IsNullOrEmpty(uri))
            {
                var segments = uri.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                for(int i = 0; i < segments.Length ; i += 2)
                {
                    result.Add(segments[i], segments[i + 1]);
                }
            }
            return result;
        }
    }
}
