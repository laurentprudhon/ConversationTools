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

        //The core attribute of ViewGenerator
        //Consists of a list of Intents, each intent containing a list of data-tree ViewNode objects
        public List<Intent> Intents;

        public ViewGenerator(Dialog dialog)
        {

            Intents = new List<Intent>();

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
            ViewNode condition = new ViewNode(node);
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
                        condition.AddChild(ReadNode(child, new ViewNode(child)));
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
        public ViewNode(DialogNode node)
        {
            Children = new List<ViewNode>();
            DisplayValues = new List<DisplayValue>();
            Type = node.Type;

            if (node.Type == DialogNodeType.DialogVariableConditions)
            {

                foreach (var condition in ((DialogVariableConditions)node).VariableConditions)
                {

                    DisplayValues.Add(new DisplayValue(condition));

                }
            }
            else
            {
                DisplayValues.Add(new DisplayValue(node));
            }
                 
        }

        //Adding a node Method
        public void AddChild(ViewNode condition)
        {
            this.Children.Add(condition);
        }

    }

    //Label and attributes of each entity
    public class DisplayValue
    {
        public string Value { get;  set; }
        public string Variable { get;  set; }
        public List<Attribute> Attributes { get; set; }

        public DisplayValue(DialogVariableCondition condition)
        {
            Value = (condition.Comparison != ConditionComparison.HasValue) ? condition.Value : "";
            Variable = condition.VariableName;
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
        public DisplayValue(DialogNode node)
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
                    Variable = " ";
                    Attributes.Add(new Attribute("title", question));

                    break;

                case DialogNodeType.FatHeadAnswers:

                    string[] URI = ((FatHeadAnswers)node).MappingUris;

                    Value = "Réponse";
                    Variable = " ";
                    string uriattribute = "";
                    int i = 0;

                    foreach (var uri in URI)
                    {
                        if (i > 0)
                        {
                            uriattribute = uriattribute + " \r\n" + uri;
                        }
                        else
                        {
                            uriattribute = uri;
                        }

                        i += 1;
                    }

                    Attributes.Add(new Attribute("title", uriattribute));

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

