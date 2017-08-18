using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dialogtool
{
    //Generates a list of intents via a Dialog object
    //each intent consists of a list of data-tree ViewNode objects
    class ViewGenerator
    {

        public struct Intent
        {
            public string Name;

            public List<ViewNode> ViewNodes;

            public IList<string> Questions;

            public Intent(string name, List<ViewNode> conditions, IList<string> questions)
            {
                Name = name;
                ViewNodes = conditions;
                Questions = questions;
            }

        }

        private AnswerStoreSimulator answerStore;

        //The core attribute of ViewGenerator
        //Consists of a list of Intents, each intent containing a list of data-tree ViewNode objects
        public List<Intent> Intents;

        public ViewGenerator(Dialog dialog, string answerstoreFile)
        {

            Intents = new List<Intent>();
            answerStore = new AnswerStoreSimulator(answerstoreFile);

            foreach (var intent in dialog.Intents.Values.OrderBy(i => i.Name))
            {
                List<ViewNode> conditions = new List<ViewNode>();

                foreach (var node in intent.ChildrenNodes)
                {
                    conditions.Add(ReadRoot(node));
                }

                Intents.Add(new Intent(intent.Name, conditions, intent.Questions));
            }
        }

        private ViewNode ReadRoot(DialogNode node)
        {
            ViewNode condition = new ViewNode(node, answerStore);
            return ReadNode(node, condition);
        }


        private ViewNode ReadNode(DialogNode node, ViewNode condition)
        {
            if (node.ChildrenNodes != null)
            {
                foreach (var child in node.ChildrenNodes)
                {
                    if (child.Type == DialogNodeType.DialogVariableConditions || child.Type == DialogNodeType.DisambiguationQuestion || child.Type == DialogNodeType.SwitchOnEntityVariables || child.Type == DialogNodeType.FatHeadAnswers)
                    {
                        condition.AddChild(ReadNode(child, new ViewNode(child, answerStore)));
                    }
                }
            }

            return condition;
        }

    }

    //Simplified data tree object constiting exclusively of view oriented dialog nodes and relevant attributes 
    public class ViewNode : DialogNode
    {
        public IList<DisplayValue> DisplayValues;
        public List<ViewNode> Children { get; set; }

        //Constructor
        public ViewNode(DialogNode node, AnswerStoreSimulator answerStore)
        {
            Children = new List<ViewNode>();
            DisplayValues = new List<DisplayValue>();
            Type = node.Type;
            List<DialogVariableAssignment> assignList = new List<DialogVariableAssignment>();

            if (node.Type == DialogNodeType.DialogVariableConditions)
            {

                foreach (var condition in ((DialogVariableConditions)node).VariableConditions)
                {
                    //If we force the value of another entity
                    //Extract the assignements
                    if (node.ChildrenNodes != null && node.ChildrenNodes.Count > 0)
                    {
                        foreach (var child in node.ChildrenNodes)
                        {
                            if (child.VariableAssignments != null && child.VariableAssignments.Count > 0)
                            {
                                foreach (var assign in child.VariableAssignments)
                                {
                                    assignList.Add(assign);
                                }
                            }

                        }
                    }


                    if (assignList.Count > 0)
                    {
                        DisplayValues.Add(new DisplayValue(condition, assignList));
                    }
                    else
                    {
                        DisplayValues.Add(new DisplayValue(condition));
                    }

                }
            }
            else
            {
                DisplayValues.Add(new DisplayValue(node, answerStore));
            }
                 
        }

        //Adding a node Method
        public void AddChild(ViewNode condition)
        {
            this.Children.Add(condition);
        }

    }

    public enum DisplayValueType
    {
        Question,
        Answer,
        Variable,
        Assign,
    }

    //Label and attributes of each entity
    public class DisplayValue
    {
        public string Value { get;  set; }
        public string Variable { get;  set; }
        public List<Attribute> Attributes { get; set; }
        public DisplayValueType Type { get; set; }
        //JS tooltip text
        public string SecondaryInfo { get; set; }
        //Extra Hide/display JS info
        public string HiddenInfo { get; set; }

        public DisplayValue(DialogVariableCondition condition)
        {
            switch (condition.Comparison)
            {
                case ConditionComparison.HasValue:
                    Value = "HasValue";
                    break;
                case ConditionComparison.Equals:
                    if (condition.Value == "")
                    {
                        Value = condition.VariableName + " absent";
                    }
                    else
                        Value = condition.Value;
                    break;
                default:
                    break;
            }

            Variable = condition.VariableName;
            Type = DisplayValueType.Variable;
            Attributes = new List<Attribute>();
            string synonyms = "";

            if (condition.EntityValue != null && condition.EntityValue.Concepts != null)
            {
                foreach (var concept in condition.EntityValue.Concepts)
                {

                    if (concept.Synonyms != null && concept.Synonyms.Count > 0)
                    {
                        foreach (var syn in concept.Synonyms)
                        {
                            synonyms = synonyms + syn + "\r\n";
                        }
                    }
                }
            }

            Attributes.Add(new Attribute("title", synonyms));

        }

        //Constructor Overload
        public DisplayValue(DialogNode node, AnswerStoreSimulator answerStore)
        {
            Value = Variable = " ";
            Attributes = new List<Attribute>();

            switch (node.Type)
            {
                case DialogNodeType.DisambiguationQuestion:

                    string question = ((DisambiguationQuestion)node).QuestionText;

                    if (((DisambiguationQuestion)node).DisambiguationOptions != null)
                    {
                        foreach (var option in ((DisambiguationQuestion)node).DisambiguationOptions)
                        {
                            question = question + " \r\n -" + option.Text;
                        }
                    }
                    else
                    {
                        question += "\r\n - Attention, absence d'option de désambiguïsation";
                    }

                    Value = "Question";
                    Type = DisplayValueType.Question;
                    Variable = " ";
                    Attributes.Add(new Attribute("title", question));

                    break;

                case DialogNodeType.FatHeadAnswers:

                    string[] URI = ((FatHeadAnswers)node).MappingUris;

                    Value = "Réponse";
                    Type = DisplayValueType.Answer;
                    Variable = " ";
                    string uriattribute = "";
                    string reponse = "";
                    int i = 0;

                    foreach (var uri in URI)
                    {                      
                        if (answerStore.GetAnswerUnitForMappingUri(uri) != null)
                        {
                            reponse = answerStore.GetAnswerUnitForMappingUri(uri).content.plainText;
                        }
                        else
                        {
                            reponse = "introuvable dans l'Answer Store";
                        }

                        if (i > 0)
                        {
                            uriattribute += "<br>" + "URI :  " + uri + " <br>" + "Réponse : <br>" + reponse;
                            HiddenInfo += "<br>" + "<i>" + uri + "</i>";
                        }
                        else
                        {
                            HiddenInfo += "<font color = \"#808080\" size = \"2\" >" + "<br>" + "<i>" + uri + "</i>";
                            uriattribute = "<font size = \"2\" >" +  "URI : " + uri + "<br>" + "Réponse : <br>" + reponse;
                        }

                        i += 1;
                    }

                    if (i>0)
                    {
                        uriattribute += "</font>";
                        HiddenInfo += "</font>";
                    }



                    Attributes.Add(new Attribute("title", uriattribute + reponse));
                    SecondaryInfo = uriattribute + reponse;

                    break;

                case DialogNodeType.RedirectToLongTail:

                    Value = "Redirection en Long Tail";
                    break;

                default:
                    Value = "root";
                    break;
            }

        }

        //Constructor Overload
        public DisplayValue(string value, string variable, Attribute attribute)
        {
            Value = value;
            Variable = variable;
            Attributes.Add(attribute);
        }

        public DisplayValue(DialogVariableCondition condition, List<DialogVariableAssignment> assignList)
        {
            switch (condition.Comparison)
            {
                case ConditionComparison.HasValue:
                    Value = "HasValue";
                    break;
                case ConditionComparison.Equals:
                    if (condition.Value == "")
                    {
                        Value = condition.VariableName + " absent";
                    }
                    else
                        Value = condition.Value;
                    break;
                default:
                    break;
            }

            Variable = condition.VariableName;
            Type = DisplayValueType.Assign;
            Attributes = new List<Attribute>();
            string synonyms = "";

            if (condition.EntityValue != null && condition.EntityValue.Concepts != null)
            {
                foreach (var concept in condition.EntityValue.Concepts)
                {

                    if (concept.Synonyms != null && concept.Synonyms.Count > 0)
                    {
                        foreach (var syn in concept.Synonyms)
                        {
                            synonyms = synonyms + syn + "\r\n";
                        }
                    }
                }
            }

            Attributes.Add(new Attribute("title", synonyms));
            SecondaryInfo = "";

            foreach (var assign in assignList)
            {
                switch (assign.Operator)
                {
                    case DialogVariableOperator.SetTo:
                        SecondaryInfo += assign.VariableName + " positionné à " + assign.Value + "<br>";
                        break;
                    default:
                        break;
                }
            }

        }


        public struct Attribute
        {
            public string Name;
            public string Value;

            public Attribute(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

    }

}

