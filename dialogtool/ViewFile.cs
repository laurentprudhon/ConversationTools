using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace dialogtool
{
    public static class ViewFile
    {
        private static List<string> Colors = new List<string>(new string[] { "darkorchid", "blue", "green", "olive", "darkorange", "red", "pink"});
        //Entity = key
        //Color = value
        private static Dictionary<string, string> EntityColor = new Dictionary<string, string>();
        private static int numberAnswer;

        public static void Write(Dialog dialog, string sourceFilePath, string answerstoreFile)
        {

            ViewGenerator viewGenerator = new ViewGenerator(dialog, answerstoreFile);
            GetColorCode(dialog);
            numberAnswer = 0;

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "   ";
            settings.OmitXmlDeclaration = true;

            using (var xw = XmlWriter.Create(sourceFilePath + ".html", settings))
            {

                xw.WriteStartElement("html");

                WriteCSS(xw);

                xw.WriteStartElement("body");

                foreach (var intent in viewGenerator.Intents)
                {
                    //Sample of questions associated with the current intent
                    string questions = "";

                    for (int i = 0; i < 5 && i < intent.Questions.Count; i++)
                    {
                        questions += "-" + intent.Questions[i] + "\r\n";
                    }

                    xw.WriteStartElement("h1");
                    xw.WriteAttributeString("title", questions);
                    xw.WriteString(intent.Name);
                    xw.WriteEndElement(); // h1           

                    WriteColorCode(xw);

                    xw.WriteStartElement("table");

                    //table visual options
                    xw.WriteAttributeString("border", "3");
                    xw.WriteAttributeString("width", "80%");
                    xw.WriteAttributeString("cellpadding", "5");
                    xw.WriteAttributeString("cellspacing", "5"); 
                    xw.WriteAttributeString("bgcolor", "#E6E6E6");

                    foreach (var condition in intent.ViewNodes)
                    {
                        WriteRoot(condition, xw);
                    }

                    xw.WriteEndElement(); // table
                }

                xw.WriteEndElement(); // body
                xw.WriteEndElement(); // html
            }

        }

        //Write the root of the current ViewNode data-tree
        private static void WriteRoot(ViewNode condition, XmlWriter xw)
        {
            xw.WriteStartElement("tr");
            WriteNode(condition, xw);
            xw.WriteEndElement();
        }

        //Recursively write the nodes of the current ViewNode data-tree 
        private static void WriteNode(ViewNode condition, XmlWriter xw)
        {
            //We don't write the root node, or the "HasValue" test
            if (condition.DisplayValues.Count > 0 && condition.DisplayValues[0].Value != "root" && condition.DisplayValues[0].Value != "HasValue")
            {
                xw.WriteStartElement("td");

                xw.WriteAttributeString("RowSpan", GetRawSpan(condition).ToString());
                xw.WriteAttributeString("bgcolor", "#FAFAFA");

                if (condition.DisplayValues[0].Attributes.Count > 0 && condition.DisplayValues[0].Type != DisplayValueType.Answer)
                {
                    xw.WriteAttributeString(condition.DisplayValues[0].Attributes[0].Name, condition.DisplayValues[0].Attributes[0].Value);
                }

                if (condition.DisplayValues.Count > 0)
                {
                    foreach (var value in condition.DisplayValues)
                    {
                        xw.WriteStartElement("div");
                        xw.WriteAttributeString("class", "tooltip");
                        xw.WriteStartElement("font");
                        if (value.Attributes.Count > 0 && value.Type != DisplayValueType.Answer)
                        {
                            foreach (var attribute in value.Attributes)
                            {
                                xw.WriteAttributeString(attribute.Name, attribute.Value);
                            }
                        }

                        xw.WriteAttributeString("color", GetColor(value));

                        //If it's an answer, we count it to identify the answer node
                        if (value.Type == DisplayValueType.Answer)
                        {
                            numberAnswer += 1;
                            xw.WriteString(value.Value + numberAnswer.ToString());
                        }
                        else
                        {
                            xw.WriteString(value.Value);
                        }

                        if (condition.DisplayValues.Count > 1)
                        {
                            xw.WriteString(" | ");
                        }

                        xw.WriteEndElement(); //font

                        if (value.Type == DisplayValueType.Answer || value.Type == DisplayValueType.Assign)
                        {
                            if (value.SecondaryInfo != "")
                            {
                                xw.WriteStartElement("span");
                                xw.WriteAttributeString("class", "tooltiptext");
                                xw.WriteRaw(value.SecondaryInfo);
                                xw.WriteEndElement(); //span
                            }

                        }

                        xw.WriteEndElement(); //div
                    }
                }

               
                xw.WriteEndElement(); //td
            }

            //As long as there's a child node, we keep going
            if (condition.Children != null && condition.Children.Count > 0)
            {
                foreach (var child in condition.Children)
                {
                    WriteNode(child, xw);
                }
            }

            //if there's no child left, it's a leaf, we end the row </tr> and start a new one <tr>
            if (condition.Children != null || condition.Children.Count > 0)
            {
                xw.WriteEndElement();
                xw.WriteStartElement("tr");

            }

        }

        //Get the number of leaves a node has
        private static int GetRawSpan(ViewNode condition)
        {

            int rowspan = ( GetNextChild(condition, 0) >=1) ? GetNextChild(condition, 0) : 1;

            return rowspan;

        }

        //used to recursively read the data-tree, summing-up the number of leaves
        private static int GetNextChild(ViewNode condition, int rowspan)
        {

            rowspan = condition.Children.Count;

            if (condition.Children != null && condition.Children.Count > 0)
            {
                foreach (var child in condition.Children)
                {                  
                    rowspan += GetNextChild(child, rowspan);
                }
            }

            return rowspan;
        }

        //Associate entity to a color code in the EntityColor Dictionnary
        private static void GetColorCode(Dialog dialog)
        {
            int i = 0;
            foreach (var entity in dialog.Entities)
            {
                EntityColor.Add(entity.Value.Name.TrimEnd("_ENTITY").ToLower(), Colors[i]);
                i += 1;
            }
        }

        //Write the color legend
        private static void WriteColorCode(XmlWriter xw)
        {
            xw.WriteStartElement("p");

            foreach (var entitycolor in EntityColor)
            {
                xw.WriteStartElement("font");
                xw.WriteAttributeString("color", entitycolor.Value);
                xw.WriteString(entitycolor.Key + " | ");
                xw.WriteEndElement(); // font
                
            }

            xw.WriteEndElement(); // p
        }

        //Colorize text with reference to the relevant entity
        private static string GetColor(DisplayValue displayValue)
        {
            string color;

            if (!EntityColor.TryGetValue(displayValue.Variable.TrimEnd("_Var").ToLower(), out color))
            {
                if (!EntityColor.TryGetValue(displayValue.Variable.TrimEnd("_Var_2").ToLower(), out color))
                {
                    color = "black";
                }
            }

            return color;

        }

        private static void WriteCSS(XmlWriter xw)
        {

            xw.WriteStartElement("style");
            xw.WriteString(".tooltip { position: relative; display: inline-block; border-bottom: 1px dotted black;}.tooltip .tooltiptext {visibility: hidden;width: 900px;background-color: #CEE3F6;color: #141907;padding: 5px 0;border-radius: 6px;position: absolute;z-index: 1;}.tooltip:hover .tooltiptext {visibility: visible;}");
            xw.WriteEndElement(); //style
            
        }

        //TrimEnd() overload using a string instead of a char[]
        private static string TrimEnd(this string input, string suffixToRemove)
        {

            if (input != null && suffixToRemove != null
              && input.EndsWith(suffixToRemove))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }
            else return input;
        }

    }
}
