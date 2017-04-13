using System;
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
                ExecuteUserInputNode(dialog, intent, questionText, intent.EntityMatches, result);
            }
            else
            {
                result.LogMessage("Intent name " + intentName + " undefined in dialog file " + dialog.FilePath);
            }

            return result;
        }

        public static DialogExecutionResult AnalyzeDisambiguationQuestion(Dialog dialog, DisambiguationQuestion question, DisambiguationOption option, string intentName)
        {
            var result = new DialogExecutionResult(question.LineNumber.ToString(), question.QuestionText, intentName);
            result.SetDisambiguationOption(option);
            
            ExecuteUserInputNode(dialog, question, option.Text, new EntityMatch[] { question.EntityMatch }, result);

            return result;
        }

        private static void ExecuteUserInputNode(Dialog dialog, DialogNode dialogNode, string userInputText, IList<EntityMatch> entityMatches, DialogExecutionResult result)
        {
            // Try to match entity values in the questions
            var entities = entityMatches.Select(entityMatch => entityMatch.Entity);
            EntityValuesMatchResult matchResult = EntityValuesMatcher.MatchEntityValues(entities, userInputText, dialog.ConceptsSynonyms, dialog.ConceptsRegex);

            // Store the result of this execution
            var nodeExecution = new MatchEntitiesNodeExecution(dialogNode, matchResult);
            result.AddDialogNodeExecution(nodeExecution);

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

            // Traverse the children nodes
            SelectChildNode(dialog, result.VariablesValues, dialogNode, null, result);
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
                                return;
                            }
                            else if (!String.IsNullOrEmpty(switchNode.EntityMatch.EntityVariableName2))
                            {
                                if (variablesValues.TryGetValue(switchNode.EntityMatch.EntityVariableName2, out varValue))
                                {
                                    if (!String.IsNullOrEmpty(varValue))
                                    {
                                        ExecuteConditionalNode(dialog, variablesValues, childNode, result);
                                        return;
                                    }
                                }
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

        public DisambiguationOption DisambiguationOption { get; private set; }
        internal void SetDisambiguationOption(DisambiguationOption option)
        {
            DisambiguationOption = option;
        }

        public IList<DialogNodeExecution> DialogNodesExecutionPath { get; private set; }
        public void AddDialogNodeExecution(DialogNodeExecution dialogNodeExecution)
        {
            DialogNodesExecutionPath.Add(dialogNodeExecution);
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
                return oldResult.DialogNodesExecutionPath[oldResult.DialogNodesExecutionPath.Count - 1].ToString() ==
                       this.DialogNodesExecutionPath[this.DialogNodesExecutionPath.Count - 1].ToString();
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
            if (EntityValuesMatchResult.ConceptSubstitutions != null && EntityValuesMatchResult.ConceptSubstitutions.Count > 0)
            {
                foreach (var substitution in EntityValuesMatchResult.ConceptSubstitutions)
                {
                    sb.Append(" > ");
                    sb.Append(substitution.ToString());
                }
            }
            if (EntityValuesMatchResult.EntityValueMatches != null && EntityValuesMatchResult.EntityValueMatches.Count > 0)
            {
                foreach (var entityValueMatch in EntityValuesMatchResult.EntityValueMatches)
                {
                    sb.Append(" > ");
                    sb.Append(entityValueMatch.ToString());
                }
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
    }
}
