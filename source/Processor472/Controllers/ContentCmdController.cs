
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Controllers {
    //
    //============================================================================================
    //      commands manually entered into html = {% {jsonFormAddonExecution} %} = called content commands in the doc
    //          format: context switch on {% and %}
    //
    //              commands:
    //                  commands append their output to an acumulator
    //                  the accumulator is passed to each addon
    //                  the accumulator is returned when all addons are finished.
    //
    //              simple syntax:
    //                  {% user firstname %} - outputs the users firstname
    //                  {% "user" "firstname" %} - double quote words, required if they have spaces
    //                  {% open "/my template.html" %} - opens the file named "my template.html" from the website root folder and outputs it
    //
    //              single command syntax:
    //                  {%{"commandName":"commandArgument"}%} -- commands with single arguments
    //                      ex: {% "user":"firstname"}%} - outputs the users firstname
    //                  {%{"commandName":{"argName1":"argValue1","argName2":"argValue2"}}%} -- commands with mulitple arguments
    //                      ex: {%{"addon":{"addon":"My Custom Addon","color":"blue }}%} - runs an addon named "My Custom Addon" with the argument color=blue
    //
    //              multiple command syntax:
    //                  {%[{"command1":"commandArgument"},{"command2":"commandArgument"}]%}
    //
    //                  addon - executes an addon
    //                      arguments:
    //                          addon: the name or guid of the addon to execute
    //                          any arguments the addon needs
    //                      ex: {% addon "my account" %}
    //                      ex: {%{"addon":{"addon":"my account","myAccountArg":"valueForArg"}}%}
    //
    //                  textbox - returns the copy in a record in copy content, includes edit icon when enabled
    //                      arguments: name
    //                      ex: {% textbox "home page footer" %}
    //                      ex: {%{"textbox":{"name":"home page footer"}}%}
    //
    //                  openCopy - returns the copy in a record in copy content
    //                      arguments: name
    //                      ex: {% opencopy "home page footer" %}
    //                      ex: {%{"opencopy":{"name":"home page footer"}}%}
    //
    //                  openLayout - returns the copy in a record in layouts
    //                      arguments: name
    //                      ex: {% openLayout "home page footer" %}
    //                      ex: {%{"openLayout":{"name":"home page footer"}}%}
    //
    //                  open - returns the body contents ofa file in the wwwRoot
    //                      arguments: name
    //                      ex: {% open "formLayout.html" %}
    //                      ex: {%{"open":{"name":"formLayout.html"}}%}
    //
    //                  import - returns the contents ofa file in the wwwRoot, adds head content to current head
    //                      arguments: name
    //                      ex: {% import "formLayout.html" %}
    //                      ex: {%{"import":{"name":"formLayout.html"}}%}
    //
    //                  user - returns the content from a field in the current user's record
    //                      arguments: field = the name of any field in people table
    //                      ex: {% user firstname %}
    //                      ex: {%{"user":{"field":"firstname"}}%}
    //
    //                  site - returns the value of a site property
    //                      arguments: name
    //                      ex: {% site "my site property" %}
    //                      ex: {%{"site":{"name":"my site property"}}%}
    //
    //                  set - performs a find and replace on the accumulator
    //                      ex: { %  {"set" : { "find":"good","replace":"great" }} % }
    //
    //                  getInner
    //                      ex: { % {"getInner" : { "find":".main-nav" }} % }
    //
    //                  setInner
    //                      ex: { % {"setInner" { "find":".left-nav","replace":"Left Navigation" }} % } - replaces the innerHtml of all elements with class "left-nav" with the text "Left Navigation"
    //
    //                  getOuter
    //                      ex: { % {"getOuter" : { "find":".main-nav" }} % } - returns the outerHtml of all elements with the class "main-nav". The outerHtml is everything inside the tag, plus the opening and closing tag.
    //
    //                  setOuter
    //                      ex: { % {"setInner" { "find":".left-nav","replace":"Left Navigation" }} % } - replaces the innerHtml of all elements with class "left-nav" with the text "Left Navigation"
    //
    // todo - integrate old docs into newer docs
    // -- older docs
    //
    //   A list of commands that create, modify and return strings
    //   the start and end with escape sequences contentReplaceEscapeStart/contentReplaceEscapeEnd
    //       {{ and }} previously
    //       {% and %} right now
    //
    //   format:
    //       {% commands %}
    //
    //    commands
    //       a single command or a JSON array of commands.
    //       if a command has arguments, the command should be a JSON object
    //           openLayout layoutName
    //
    //       one command, no arguments -- non JSON
    //               {% user %}
    //       one command, one argument -- non JSON
    //               {% user "firstname" %}
    //
    //       one command, no arguments -- JSON command array of one
    //               {% [ "user" ] %}
    //               cmdList[0] = "user"
    //
    //       two commands, no arguments -- JSON command array
    //               {% [
    //                       "user",
    //                       "user"
    //                   ] %}
    //               cmdList[0] = "user"
    //               cmdList[1] = "user"
    //
    //       one command, one argument -- JSON object for command
    //               {% [
    //                       {
    //                           "cmd": "layout",
    //                           "arg": "mylayout"
    //                       }
    //                   ] %}
    //               cmdList[0].cmd = layout
    //               cmdList[0].arg = "mylayout"
    //
    //       one command, two arguments
    //               {% [
    //                       {
    //                           "cmd": "set",
    //                           "arg": {
    //                               "find":"$fpo$",
    //                               "replace":"Some Content"
    //                       }
    //                   ] %}
    //               cmdList[0].cmd = "replace"
    //               cmdList[0].arg.find = "$fpo$"
    //               cmdList[0].arg.replace = "Some Content"
    //
    //       two commands, two arguments
    //               {% [
    //                       {
    //                           "cmd": "import",
    //                           "arg": "myTemplate.html"
    //                       },
    //                       {
    //                           "cmd": "setInner",
    //                           "arg": {
    //                               "find":".contentBoxClass",
    //                               "replace":"{% addon contentBox %}"
    //                       }
    //                   ] %}
    //               cmdList[0].cmd = "import"
    //               cmdList[0].arg = "myTemplate.html"
    //               cmdList[1].cmd = "setInner"
    //               cmdList[0].arg.find = ".contntBoxClass"
    //               cmdList[0].arg.replace = "{% addon contentBox %}"
    //
    //           import htmlFile
    //           importVirtual htmlFile
    //           open textFile
    //           openVirtual webfilename
    //           addon contentbox( JSON-Object-optionstring-list )
    //           set find replace
    //           setInner findLocation replace
    //           setOuter findLocation replace
    //           user firstname
    //           site propertyname
    public static class ContentCmdController {
        //
        //============================================================================================
        /// <summary>
        /// Execute context commands in the source text
        /// </summary>
        /// <param name="core"></param>
        /// <param name="src"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        public static string executeContentCommands(CoreController core, string src, CPUtilsBaseClass.addonContext Context) {
            try {
                //
                // -- fast exit for content w/o cmds
                if(string.IsNullOrWhiteSpace(src)) { return src;  };
                if (src.IndexOf(contentReplaceEscapeStart).Equals(-1)) { return src; }
                if (src.IndexOf(contentReplaceEscapeEnd).Equals(-1)) { return src; }
                //
                int Ptr = 0;
                var result = new StringBuilder();
                int ptrLast = 1;
                do {
                    string Cmd = "";
                    int posOpen = GenericController.strInstr(ptrLast, src, contentReplaceEscapeStart);
                    Ptr = posOpen;
                    int posClose = 0;
                    if (Ptr == 0) {
                        //
                        // not found, copy the rest of src to dst
                        //
                    } else {
                        //
                        // scan until we have passed all double and single quotes that are before the next
                        //
                        bool notFound = true;
                        do {
                            posClose = GenericController.strInstr(Ptr, src, contentReplaceEscapeEnd);
                            if (posClose == 0) {
                                //
                                // brace opened but no close, forget the open and exit
                                //
                                posOpen = 0;
                                notFound = false;
                            } else {
                                int posDq = Ptr;
                                string escape = null;
                                do {
                                    posDq = GenericController.strInstr(posDq + 1, src, "\"");
                                    escape = "";
                                    if (posDq > 0) {
                                        escape = src.Substring(posDq - 2, 1);
                                    }
                                } while (escape == "\\");
                                int posSq = Ptr;
                                do {
                                    posSq = GenericController.strInstr(posSq + 1, src, "'");
                                    escape = "";
                                    if (posSq > 0) {
                                        escape = src.Substring(posSq - 2, 1);
                                    }
                                } while (escape == "\\");
                                int posNextQuote = getFirstNonZeroInteger(posSq, posDq);
                                if (posNextQuote == 0) {
                                    notFound = false;
                                } else {
                                    //
                                    // posSq is before posDq
                                    //
                                    string nextQuoteChr = (posNextQuote == posSq) ? "'" : "\"";
                                    if (posNextQuote > posClose) {
                                        notFound = false;
                                    } else {
                                        //
                                        // skip forward to the next non-escaped sq
                                        //
                                        do {
                                            posNextQuote = GenericController.strInstr(posNextQuote + 1, src, nextQuoteChr);
                                            escape = "";
                                            if (posNextQuote > 0) {
                                                escape = src.Substring(posNextQuote - 2, 1);
                                            }
                                        } while (escape == "\\");
                                        Ptr = posNextQuote + 1;
                                    }
                                    break;
                                }
                            }
                        } while (notFound);
                    }
                    if (posOpen <= 0) {
                        //
                        // no cmd found, add from the last ptr to the end
                        //
                        result.Append(src.Substring(ptrLast - 1));
                        Ptr = -1;
                    } else {
                        //
                        // cmd found, process it and add the results to the dst
                        //
                        Cmd = src.Substring(posOpen + 1, (posClose - posOpen - 2));
                        //
                        // -- when cmd entered through wysiwyg, it is html encoded
                        Cmd = HtmlController.decodeHtml(Cmd);
                        //
                        string cmdResult = executeSingleCommand(core, Cmd, Context);
                        result.Append(src.Substring(ptrLast - 1, posOpen - ptrLast) + cmdResult);
                        Ptr = posClose + 2;
                    }
                    ptrLast = Ptr;
                } while (Ptr > 1);
                //
                return result.ToString();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //=================================================================================================================
        /// <summary>
        /// convert a single command in the command formats to call the execute
        /// </summary>
        private static string executeSingleCommand(CoreController core, string cmdSrc, CPUtilsBaseClass.addonContext Context) {
            try {
                //
                // accumulator gets the result of each cmd, then is passed to the next command to filter
                List<object> cmdCollection = null;
                Dictionary<string, object> cmdDef = null;
                Dictionary<string, object> cmdArgDef = new Dictionary<string, object>();
                //
                cmdSrc = cmdSrc.Trim(' ');
                string whiteChrs = Environment.NewLine + "\t ";
                bool trimming;
                do {
                    trimming = false;
                    int trimLen = cmdSrc.Length;
                    if (trimLen > 0) {
                        string leftChr = cmdSrc.left(1);
                        string rightChr = cmdSrc.Substring(cmdSrc.Length - 1);
                        if (GenericController.strInstr(1, whiteChrs, leftChr) != 0) {
                            cmdSrc = cmdSrc.Substring(1);
                            trimming = true;
                        }
                        if (GenericController.strInstr(1, whiteChrs, rightChr) != 0) {
                            cmdSrc = cmdSrc.left(cmdSrc.Length - 1);
                            trimming = true;
                        }
                    }
                } while (trimming);
                string CmdAccumulator = "";
                if (!string.IsNullOrEmpty(cmdSrc)) {
                    Dictionary<string, object>.KeyCollection dictionaryKeys = null;
                    object itemObject = null;
                    object itemVariant = null;
                    Dictionary<string, object> cmdObject = null;
                    //
                    cmdCollection = new List<object>();
                    if ((cmdSrc.left(1) == "{") && (cmdSrc.Substring(cmdSrc.Length - 1) == "}")) {
                        //
                        // JSON is a single command in the form of an object, like: ( "import" : "test.html" )
                        //
                        Dictionary<string, object> cmdDictionary;
                        try {
                            cmdDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(cmdSrc);
                        } catch (Exception ex) {
                            logger.Error(ex, $"{core.logCommonMessage}");
                            throw;
                        }
                        //
                        dictionaryKeys = cmdDictionary.Keys;
                        foreach (string KeyWithinLoop in dictionaryKeys) {
                            if (cmdDictionary[KeyWithinLoop] != null) {
                                cmdObject = new Dictionary<string, object>();
                                itemObject = cmdDictionary[KeyWithinLoop];
                                cmdObject.Add(KeyWithinLoop, itemObject);
                                cmdCollection.Add(cmdObject);
                            } else {
                                cmdObject = new Dictionary<string, object>();
                                itemVariant = cmdDictionary[KeyWithinLoop];
                                cmdObject.Add(KeyWithinLoop, itemVariant);
                                cmdCollection.Add(cmdObject);
                            }
                        }
                    } else if ((cmdSrc.left(1) == "[") && (cmdSrc.Substring(cmdSrc.Length - 1) == "]")) {
                        //
                        // JSON is a command list in the form of an array, like: [ "clear" , { "import": "test.html" },{ "open" : "myfile.txt" }]
                        //
                        cmdCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<List<object>>(cmdSrc);
                    } else {
                        //
                        // a single text command without JSON wrapper, like
                        //   open myfile.html
                        //   open "myfile.html"
                        //   "open" "myfile.html"
                        //   "content box"
                        //   all other posibilities are syntax errors
                        //
                        string cmdText = cmdSrc.Trim(' ');
                        string cmdArg = "";
                        if (cmdText.left(1) == "\"") {
                            //
                            // cmd is quoted
                            //   "open"
                            //   "Open" file
                            //   "Open" "file"
                            //
                            int Pos = GenericController.strInstr(2, cmdText, "\"");
                            if (Pos <= 1) {
                                throw new GenericException("Error parsing content command [" + cmdSrc + "], expected a close quote around position " + Pos);
                            } else {
                                if (Pos == cmdText.Length) {
                                    //
                                    // cmd like "open"
                                    //
                                    cmdArg = "";
                                    cmdText = cmdText.Substring(1, Pos - 2);
                                } else if (cmdText.Substring(Pos, 1) != " ") {
                                    //
                                    // syntax error, must be a space between cmd and argument
                                    //
                                    throw new GenericException("Error parsing content command [" + cmdSrc + "], expected a space between command and argument around position " + Pos);
                                } else {
                                    cmdArg = (cmdText.Substring(Pos)).Trim(' ');
                                    cmdText = cmdText.Substring(1, Pos - 2);
                                }
                            }

                        } else {
                            //
                            // no quotes, can be
                            //   open
                            //   open file
                            //
                            int Pos = GenericController.strInstr(1, cmdText, " ");
                            if (Pos > 0) {
                                cmdArg = cmdSrc.Substring(Pos);
                                cmdText = (cmdSrc.left(Pos - 1)).Trim(' ');
                            }
                        }
                        if (cmdArg.left(1) == "\"") {
                            //
                            // cmdarg is quoted
                            //
                            int Pos = GenericController.strInstr(2, cmdArg, "\"");
                            if (Pos <= 1) {
                                throw new GenericException("Error parsing JSON command list, expected a quoted command argument, command list [" + cmdSrc + "]");
                            } else {
                                cmdArg = cmdArg.Substring(1, Pos - 2);
                            }
                        }
                        if ((cmdArg.left(1) == "{") && (cmdArg.Substring(cmdArg.Length - 1) == "}")) {
                            //
                            // argument is in the form of an object, like: ( "text name": "my text" )
                            //
                            object cmdDictionaryOrCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(cmdArg);
                            string cmdDictionaryOrCollectionTypeName = cmdDictionaryOrCollection.GetType().FullName.ToLowerInvariant();
                            if (cmdDictionaryOrCollectionTypeName.left(37) != "system.collections.generic.dictionary") {
                                throw new GenericException("Error parsing JSON command argument list, expected a single command, command list [" + cmdSrc + "]");
                            } else {
                                //
                                // create command array of one command
                                //
                                cmdCollection.Add(cmdDictionaryOrCollection);
                            }
                            cmdDef = new Dictionary<string, object> {
                                { cmdText, cmdDictionaryOrCollection }
                            };
                            cmdCollection = new List<object> {
                                cmdDef
                            };
                        } else {
                            //
                            // command and arguments are strings
                            //
                            cmdDef = new Dictionary<string, object> {
                                { cmdText, cmdArg }
                            };
                            cmdCollection = new List<object> {
                                cmdDef
                            };
                        }
                    }
                    //
                    // execute the commands in the JSON cmdCollection
                    //
                    foreach (object srcCmd in cmdCollection) {

                        //
                        // repeat for all commands in the collection:
                        // convert each command in the command array to a cmd string, and a cmdArgDef dictionary
                        // each cmdStringOrDictionary is a command. It may be:
                        //   A - "command"
                        //   B - { "command" }
                        //   C - { "command" : "single-default-argument" }
                        //   D - { "command" : { "name" : "The Name"} }
                        //   E - { "command" : { "name" : "The Name" , "secondArgument" : "secondValue" } }
                        //
                        //
                        // -- these inner objects are newtonsoft jobject. serialize and deserialize
                        string tmp = Newtonsoft.Json.JsonConvert.SerializeObject(srcCmd);
                        object cmd;
                        if(tmp.left(1).Equals("{") && tmp.right(1).Equals("}")) {
                            cmd = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(tmp);
                        } else {
                            cmd = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(tmp);
                        }
                        //
                        string cmdTypeName = cmd.GetType().FullName.ToLowerInvariant();
                        string cmdText = "";
                        if (cmdTypeName == "system.string") {
                            //
                            // case A & B, the cmdDef is a string
                            //
                            cmdText = (string)cmd;
                            cmdArgDef = new Dictionary<string, object>();
                        } else if (cmdTypeName.left(37) == "system.collections.generic.dictionary") {
                            //
                            // cases C-E, (0).key=cmd, (0).value = argument (might be string or object)
                            //
                            cmdDef = (Dictionary<string, object>)cmd;
                            if (cmdDef.Count != 1) {
                                //
                                // syntax error
                                //
                            } else {
                                string cmdDefKey = cmdDef.Keys.First();
                                string cmdDefValueTypeName = cmdDef[cmdDefKey].GetType().FullName.ToLowerInvariant();
                                //
                                // command is the key for these cases
                                //
                                cmdText = cmdDefKey;
                                if (cmdDefValueTypeName == "system.string") {
                                    //
                                    // command definition with default argument
                                    //
                                    cmdArgDef = new Dictionary<string, object> {
                                        { "default", cmdDef[cmdDefKey] }
                                    };
                                } else if ((cmdDefValueTypeName == "dictionary") || (cmdDefValueTypeName == "dictionary(of string,object)") || (cmdTypeName.left(37) == "system.collections.generic.dictionary")) {
                                    //
                                    // -- json jobject that mimiks dictionary. serialize it and re-deserialize 
                                    string asdfasdf = Newtonsoft.Json.JsonConvert.SerializeObject(cmdDef[cmdDefKey]);
                                    cmdArgDef = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(asdfasdf);
                                } else {
                                    //
                                    // syntax error, bad command
                                    //
                                    throw new GenericException("Error parsing JSON command list, , command list [" + cmdSrc + "]");
                                }
                            }
                        } else {
                            //
                            // syntax error
                            //
                            throw new GenericException("Error parsing JSON command list, , command list [" + cmdSrc + "]");
                        }
                        //
                        // execute the cmd with cmdArgDef dictionary
                        //
                        switch (GenericController.toLCase(cmdText)) {
                            case "textbox": {
                                    //
                                    // Opens a textbox addon (patch for text box name being "text name" so it requies json)copy content record
                                    //
                                    // arguments
                                    //   name: copy content record
                                    // default
                                    //   name
                                    //
                                    CmdAccumulator = "";
                                    string ArgName = "";
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        switch (kvp.Key.ToLowerInvariant()) {
                                            case "name":
                                            case "default":
                                                ArgName = (string)kvp.Value;
                                                break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(ArgName)) {
                                        CmdAccumulator = core.html.getContentCopy(ArgName, "copy content", core.session.user.id, true, core.session.isAuthenticated);
                                    }
                                    break;
                                }
                            case "opencopy": {
                                    //
                                    // Opens a copy content record
                                    //
                                    // arguments
                                    //   name: layout record name
                                    // default
                                    //   name
                                    //
                                    CmdAccumulator = "";
                                    string ArgName = "";
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        switch (kvp.Key.ToLowerInvariant()) {
                                            case "name":
                                            case "default":
                                                ArgName = (string)kvp.Value;
                                                break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(ArgName)) {
                                        CmdAccumulator = core.html.getContentCopy(ArgName, "copy content", core.session.user.id, true, core.session.isAuthenticated);
                                    }
                                    break;
                                }
                            case "openlayout": {
                                    //
                                    // Opens a layout record
                                    //
                                    // arguments
                                    //   name: layout record name
                                    // default
                                    //   name
                                    //
                                    CmdAccumulator = "";
                                    string ArgName = "";
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        switch (kvp.Key.ToLowerInvariant()) {
                                            case "name":
                                            case "default":
                                                ArgName = (string)kvp.Value;
                                                break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(ArgName)) {
                                        DataTable dt = core.db.executeQuery("select layout from ccLayouts where name=" + DbController.encodeSQLText(ArgName));
                                        if (dt != null) {
                                            CmdAccumulator = GenericController.encodeText(dt.Rows[0]["layout"]);
                                        }
                                        dt.Dispose();
                                    }
                                    break;
                                }
                            case "open": {
                                    //
                                    // Opens a file in the wwwPath
                                    //
                                    // arguments
                                    //   name: filename
                                    // default
                                    //   name
                                    //
                                    CmdAccumulator = "";
                                    string ArgName = "";
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        switch (kvp.Key.ToLowerInvariant()) {
                                            case "name":
                                            case "default":
                                                ArgName = (string)kvp.Value;
                                                break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(ArgName)) {
                                        CmdAccumulator = core.wwwFiles.readFileText(ArgName);
                                    }
                                    break;
                                }
                            case "userproperty":
                            case "user": {
                                    CmdAccumulator = "";
                                    string ArgName = "";
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        switch (kvp.Key.ToLowerInvariant()) {
                                            case "name":
                                            case "default":
                                                ArgName = (string)kvp.Value;
                                                break;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(ArgName)) {
                                        CmdAccumulator = core.userProperty.getText(ArgName, "");
                                    }
                                    break;
                                }
                            case "siteproperty":
                            case "site": {
                                    //
                                    // returns a site property
                                    //
                                    // arguments
                                    //   name: the site property name
                                    // default argument
                                    //   name
                                    //
                                    CmdAccumulator = "";
                                    string ArgName = "";
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        switch (kvp.Key.ToLowerInvariant()) {
                                            case "name":
                                            case "default": {
                                                    ArgName = (string)kvp.Value;
                                                    break;
                                                }
                                            default: {
                                                    // do nothing
                                                    break;
                                                }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(ArgName)) {
                                        CmdAccumulator = core.siteProperties.getText(ArgName, "");
                                    }
                                    break;
                                }
                            case "runaddon":
                            case "executeaddon":
                            case "addon": {
                                    //
                                    // execute an add-on
                                    //
                                    string addonName = "";
                                    Dictionary<string, string> addonArgDict = new Dictionary<string, string>();
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        if (kvp.Key.ToLowerInvariant().Equals("addon")) {
                                            addonName = kvp.Value.ToString();
                                        } else {
                                            addonArgDict.Add(kvp.Key, kvp.Value.ToString());
                                        }
                                    }
                                    addonArgDict.Add("cmdAccumulator", CmdAccumulator);
                                    AddonModel addon = core.cacheRuntime.addonCache.createByUniqueName(addonName);
                                    var executeContext = new Contensive.BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                        addonType = Context,
                                        cssContainerClass = "",
                                        cssContainerId = "",
                                        hostRecord = new Contensive.BaseClasses.CPUtilsBaseClass.addonExecuteHostRecordContext {
                                            contentName = "",
                                            fieldName = "",
                                            recordId = 0
                                        },
                                        argumentKeyValuePairs = addonArgDict,
                                        errorContextMessage = "calling Addon [" + addonName + "] during content cmd execution"
                                    };
                                    if (addon == null) {
                                        logger.Error(new GenericException("Add-on [" + addonName + "] could not be found executing command in content [" + cmdSrc + "]"), $"{core.logCommonMessage}");
                                    } else {
                                        CmdAccumulator = core.addon.execute(addon, executeContext);
                                    }

                                    break;
                                }
                            default: {
                                    //
                                    // execute an add-on
                                    //
                                    string addonName = cmdText;
                                    Dictionary<string, string> addonArgDict = new Dictionary<string, string>();
                                    foreach (KeyValuePair<string, object> kvp in cmdArgDef) {
                                        addonArgDict.Add(kvp.Key, kvp.Value.ToString());
                                    }
                                    addonArgDict.Add("cmdAccumulator", CmdAccumulator);
                                    var executeContext = new CPUtilsBaseClass.addonExecuteContext {
                                        addonType = Context,
                                        cssContainerClass = "",
                                        cssContainerId = "",
                                        hostRecord = new CPUtilsBaseClass.addonExecuteHostRecordContext {
                                            contentName = "",
                                            fieldName = "",
                                            recordId = 0
                                        },
                                        argumentKeyValuePairs = addonArgDict,
                                        errorContextMessage = "calling Addon [" + addonName + "] during content cmd execution"
                                    };
                                    AddonModel addon = core.cacheRuntime.addonCache.createByUniqueName(addonName);
                                    CmdAccumulator = core.addon.execute(addon, executeContext);
                                    break;
                                }
                        }
                    }
                }
                return CmdAccumulator;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
