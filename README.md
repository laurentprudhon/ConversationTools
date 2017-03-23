# dialogtool overview

This tool provides a set of utilities to assist development of big Dialog configuration files.

It is only useful if the dialogs are built from a combination of 8 patterns :
- Match intent and entities
- Switch on one entity values
- Dialog variables conditions
- Diasambiguation question
- Fat head answer / Long tail answer / Direct answer
- Go to node

# dialogtool syntax

dialogtool gensource dialog\savings_0703.xml
>  generates compact code from dialog file => source\savings_0703.code.xml
>  (and a template => source\savings_0703.template.xml)

dialogtool gendialog  source\savings_0703.code.xml
>  generates dialog file from compact code => dialog\savings_0703.xml
>  (using a template => source\savings_0703.template.xml)

dialogtool check dialog\savings_0703.xml
dialogtool check source\savings_0703.code.xml
>  checks source or dialog file consistency => result\savings_0703.errors.csv

dialogtool view dialog\savings_0703.xml
dialogtool view source\savings_0703.code.xml
>  generates HTML view of dialog => result\savings_0703.view.html

dialogtool answers dialog\savings_0703.xml
dialogtool answers source\savings_0703.code.xml
>  extracts answers mapping URIs from dialog => result\savings_0703.answers.csv

dialogtool debug dialog\savings_0703.xml input\questions1.csv
dialogtool debug source\savings_0703.code.xml input\questions1.csv
>  explains dialog behavior => result\savings_0703.questions1.debug.html
>  for a table of [questions | intents] in csv file (input\questions1.csv)

dialogtool compare source\savings_0703-v2.code.xml source\savings_0703-v1.code.xml input\questions.csv
>  compare answers and dialog behavior for 2 versions of code file
>  on a table of [questions | intents] in csv file (input\questions.csv)
>  => savings_0703-v2.savings_0703-v1.questions.compare.csv