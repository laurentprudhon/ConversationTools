using System;
using System.Collections.Generic;
using System.Text;

namespace dialogtool
{
    public abstract class DialogNode
    {
        public string Id { get; set; }
        public DialogNodeType Type { get; internal set; }

        public DialogNode ParentNode { get; protected set; }
        public IList<DialogNode> ChildrenNodes { get; protected set; }

        public IList<DialogVariableAssignment> VariableAssignments { get; private set; }
        public void AddVariableAssignment(DialogVariableAssignment variableAssignment)
        {
            if (VariableAssignments == null) VariableAssignments = new List<DialogVariableAssignment>();
            VariableAssignments.Add(variableAssignment);
        }

        public IList<DialogNode> DialogNodeReferences { get; private set; }
        internal void AddDialogNodeReference(DialogNode dialogNode)
        {
            if (DialogNodeReferences == null) DialogNodeReferences = new List<DialogNode>();
            DialogNodeReferences.Add(dialogNode);
        }

        internal int LineNumber { get; set; }
    }

    public enum DialogNodeType
    {
        MatchIntentAndEntites,
        DialogVariableConditions,
        SwitchOnEntityVariables,
        DisambiguationQuestion,
        FatHeadAnswers,
        RedirectToLongTail,
        DirectAnswer,
        GotoNode,
        GotoNext,
    }

    public class MatchIntentAndEntities : DialogNode
    {
        public MatchIntentAndEntities(string folder, string intentName)
        {
            Folder = folder;
            Name = intentName;

            Type = DialogNodeType.MatchIntentAndEntites;
            ParentNode = null;
            ChildrenNodes = new List<DialogNode>();
            EntityMatches = new List<EntityMatch>();
        }

        public string Folder { get; private set; }
        public string Name { get; private set; }
        public IList<string> Questions { get; set; }

        public IList<EntityMatch> EntityMatches { get; private set; }

        internal void AddEntityMatch(EntityMatch entityMatch)
        {
            EntityMatches.Add(entityMatch);            
        }

        public override string ToString()
        {
            return "Intent:" + Name;
        }
    }

    public class EntityMatch
    {
        public EntityMatch(string entityName, string entityVariableName1, string entityVariableName2)
        {
            EntityName = entityName;
            EntityVariableName1 = entityVariableName1;
            EntityVariableName2 = entityVariableName2;
        }

        public string EntityName { get; private set; }
        public Entity Entity { get; internal set; }

        public string EntityVariableName1 { get; private set; }
        public DialogVariable EntityVariable1 { get; internal set; }
        public string EntityVariableName2 { get; private set; }
        public DialogVariable EntityVariable2 { get; internal set; }
        
        internal int LineNumber { get; set; }
    }

    public class SwitchOnEntityVariables : DialogNode
    {
        public SwitchOnEntityVariables(DialogNode parentNode, EntityMatch entityMatch)
        {
            Type = DialogNodeType.SwitchOnEntityVariables;
            ParentNode = parentNode;
            ChildrenNodes = new List<DialogNode>();
            EntityMatch = entityMatch;
        }

        public EntityMatch EntityMatch { get; private set; }

        public override string ToString()
        {
            return "SwitchOn:" + EntityMatch.EntityName;
        }
    }

    public class DialogVariableConditions : DialogNode
    {
        public DialogVariableConditions(DialogNode parentNode, IList<DialogVariableCondition> variableConditions, ConditionOperator @operator)
        {
            Type = DialogNodeType.DialogVariableConditions;
            ParentNode = parentNode;
            ChildrenNodes = new List<DialogNode>();
            VariableConditions = variableConditions;
            Operator = @operator;
        }

        public IList<DialogVariableCondition> VariableConditions { get; private set; }
        public ConditionOperator Operator { get; private set; }

        public string Expression
        {
            get
            {
                var op = Operator.ToString().ToLower();
                string expression = null;
                foreach (var cond in VariableConditions)
                {
                    if (expression != null)
                    {
                        expression += " " + op + " ";
                    }
                    if (cond.Comparison == ConditionComparison.HasValue)
                    {
                        expression += cond.VariableName + " has value";
                    }
                    else
                    {
                        expression += cond.VariableName + "='" + cond.Value + "'";
                    }
                }
                return expression;
            }
        }

        public override string ToString()
        {
            return "If:" + Expression;
        }
    }

    public class DialogVariableCondition
    {
        public DialogVariableCondition(string variableName, ConditionComparison comparison, string value)
        {
            VariableName = variableName;
            Comparison = comparison;
            Value = value;
        }

        public string VariableName { get; private set; }
        public DialogVariable Variable { get; private set; }

        public ConditionComparison Comparison { get; private set; }

        public DialogConditionType Type { get; private set; }

        public string Value { get; private set; }
        
        public Entity Entity { get; private set; }
        public EntityValue EntityValue { get; private set; }

        public void SetVariableAndEntityValue(DialogVariable variable, EntityValue entityValue)
        {
            Variable = variable;
            if(entityValue != null)
            {
                Type = DialogConditionType.EntityValueCondition;
                Entity = entityValue.Entity;
                EntityValue = entityValue;
            }
        }
    }

    public enum DialogConditionType
    {
        VariableCondition,
        EntityValueCondition
    }

    public enum ConditionComparison
    {
        Equals,
        HasValue
    }

    public enum ConditionOperator
    {
        And,
        Or
    }
    
    public class DisambiguationQuestion : DialogNode
    {
        public DisambiguationQuestion(DialogNode parentNode, string questionExpression, string questionText)
        {
            Type = DialogNodeType.DisambiguationQuestion;
            ParentNode = parentNode;
            ChildrenNodes = new List<DialogNode>();

            QuestionExpression = questionExpression;
            QuestionText = questionText;
        }

        public string QuestionExpression { get; private set; }
        public string QuestionText { get; private set; }
        public IList<DisambiguationOption> DisambiguationOptions { get; private set; }

        public EntityMatch EntityMatch { get; private set; }

        public void SetEntityMatchAndDisambiguationOptions(EntityMatch entityMatch, IList<string> disambiguationOptionsText, Dialog dialog)
        {
            EntityMatch = entityMatch;
            if (entityMatch == null)
            {
                dialog.LogMessage(LineNumber, MessageType.IncorrectPattern, "Disambiguation question \"" + QuestionText + "\" without any entity match => dead end");
            }
            else
            {
                if (entityMatch.EntityVariable1 != null)
                {
                    entityMatch.EntityVariable1.AddDialogNodeReference(this, VariableReferenceType.Write);
                }
                if (entityMatch.EntityVariable2 != null)
                {
                    entityMatch.EntityVariable2.AddDialogNodeReference(this, VariableReferenceType.Write);
                }
            }
            
            DisambiguationOptions = new List<DisambiguationOption>();
            if (disambiguationOptionsText == null || disambiguationOptionsText.Count == 0)
            {
                dialog.LogMessage(LineNumber, MessageType.IncorrectPattern, "Disambiguation question \"" + QuestionText + "\" doesn't provide options to guide the user");
            }
            else if(entityMatch != null)
             {
                foreach (var optionText in disambiguationOptionsText)
                {
                    var entityValuesMatchResult = dialog.MatchEntityValueInOptionText(this, entityMatch.Entity, optionText);
                    if (entityValuesMatchResult.EntityValues.Count == 0)
                    {
                        string message = "Option \"" + optionText + "\" for question \"" + QuestionText.Trim(' ', '\r', '\n') + "\" can't be matched with any entity value for entity " + entityMatch.Entity.Name;
                        if (entityValuesMatchResult.ConceptSubstitutions != null)
                        {
                            foreach (var conceptSubstitution in entityValuesMatchResult.ConceptSubstitutions)
                            {
                                message += ",  " + conceptSubstitution.ToString();
                            }
                        }
                        var suggestedOptionText = entityMatch.Entity.SuggestEntityValueFromText(optionText);
                        if(suggestedOptionText != null && !suggestedOptionText.Equals(optionText, StringComparison.InvariantCultureIgnoreCase))
                        {
                            message += ", you may want to replace the option text with \"" + suggestedOptionText + "\"";
                        }
                        dialog.LogMessage(LineNumber, MessageType.InvalidReference, message);
                    }
                    else
                    {
                        var matchedEntityValue = entityValuesMatchResult.EntityValues[0];
                        DisambiguationOptions.Add(new DisambiguationOption(optionText, matchedEntityValue));
                        matchedEntityValue.AddDialogNodeReference(this);
                    }
                }
            }
        }

        public override string ToString()
        {
            var txt = "DisambiguationQuestion:";
            bool isFirst = true;
            foreach (var option in DisambiguationOptions)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    txt += " or ";
                }
                txt += option.EntityValue.Entity.Name + "='" + option.EntityValue.Name + "'";                
            }
            return txt;
        }
    }

    public class DisambiguationOption
    {
        public DisambiguationOption(string text, EntityValue entityValue)
        {
            Text = text;
            EntityValue = entityValue;
        }

        public string Text { get; private set; }
        public EntityValue EntityValue { get; private set; }
    }

    public class GotoNode : DialogNode
    {
        public GotoNode(DialogNode parentNode, string targetNodeId, IMessageCollector errors) :
            this(parentNode, targetNodeId, null, null, errors)
        { }

        public GotoNode(DialogNode parentNode, string targetNodeId, string messageExpression, string messageText, IMessageCollector errors)
        {
            Type = DialogNodeType.GotoNode;
            ParentNode = parentNode;
            ChildrenNodes = null;
            TargetNodeId = targetNodeId;
            MessageExpression = messageExpression;
            MessageText = messageText;            
        }

        public string TargetNodeId { get; private set; }
        public DialogNode TargetNode { get; set; }

        public void CheckTargetNodeId(IMessageCollector errors)
        {
            if (String.IsNullOrEmpty(TargetNodeId))
            {
                errors.LogMessage(LineNumber, MessageType.IncorrectPattern, "Goto pattern without target node reference => dead end");
            }
        }

        public string MessageExpression { get; private set; }
        public string MessageText { get; private set; }

        public override string ToString()
        {
            return "Goto:" + TargetNodeId;
        }
    }

    public class DirectAnswer : GotoNode
    {
        public DirectAnswer(DialogNode parentNode, string targetNodeId, string messageExpression, string messageText, IMessageCollector errors) :
            base(parentNode, targetNodeId, messageExpression, messageText, errors)
        {
            Type = DialogNodeType.DirectAnswer;
        }

        public override string ToString()
        {
            return "DirectAnswer:" + MessageText;
        }
    }

    public class FatHeadAnswers : GotoNode
    {
        public FatHeadAnswers(DialogNode parentNode, string targetNodeId, string messageExpression, string messageText, IMessageCollector errors) :
            base(parentNode, targetNodeId, messageExpression, messageText, errors)
        {
            Type = DialogNodeType.FatHeadAnswers;
        }

        public DialogVariable[] EntityVariablesNotExplicitlySet { get; private set; }
        public string[] MappingUris { get; private set; }

        public void GenerateMappingUris(DialogVariablesSimulator dialogVariables, MappingUriGenerator.MappingUriConfig mappingUriConfig)
        {
            EntityVariablesNotExplicitlySet = dialogVariables.ResetEntityVariablesNotExplicitlySet(MappingUriGenerator.GetEntityVariables(mappingUriConfig));
            bool redirectToLongTail;
            MappingUris = MappingUriGenerator.GenerateMappingURIs(dialogVariables, mappingUriConfig, out redirectToLongTail);
            if (redirectToLongTail)
            {
                Type = DialogNodeType.RedirectToLongTail;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("FatHeadAnswers");
            if (MappingUris != null && MappingUris.Length > 0)
            {
                sb.Append(':');
                bool isFirst = true;
                foreach(string mappingUri in MappingUris)
                {
                    if(isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        sb.Append("|");
                    }
                    sb.Append(mappingUri);
                }
            }
            return sb.ToString();
        }
    }

    public class RedirectToLongTail : GotoNode
    {
        public RedirectToLongTail(DialogNode parentNode, string targetNodeId, string messageExpression, string messageText, IMessageCollector errors) :
            base(parentNode, targetNodeId, messageExpression, messageText, errors)
        {
            Type = DialogNodeType.RedirectToLongTail;
        }

        public override string ToString()
        {
            return "RedirectToLongTail";
        }
    }
}
