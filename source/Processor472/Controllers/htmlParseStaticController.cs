
using System;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Exceptions;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Tools used to assemble html document elements. This is not a storage for assembling a document (see docController)
    /// </summary>
    public static class HtmlParseStaticController {
        //
        //====================================================================================================
        /// <summary>
        /// setOuter
        /// </summary>
        /// <param name="ignore"></param>
        /// <param name="layout"></param>
        /// <param name="Key"></param>
        /// <param name="textToInsert"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string setOuter(CoreController core, string layout, string Key, string textToInsert) {
            string returnValue = "";
            try {
                if (string.IsNullOrEmpty(Key)) {
                    returnValue = textToInsert;
                } else {
                    returnValue = layout;
                    int posStart = getTagStartPos(core, layout, 1, Key);
                    if (posStart != 0) {
                        int posEnd = getTagEndPos(core, layout, posStart);
                        if (posEnd > 0) {
                            //
                            // seems like these are the correct positions here.
                            //
                            returnValue = layout.left( posStart - 1) + textToInsert + layout.Substring(posEnd - 1);
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnValue;
        }
        //
        //====================================================================================================
        public static  string setInner(CoreController core, string layout, string Key, string textToInsert) {
            string returnValue = "";
            try {
                int posStart = 0;
                int posEnd = 0;
                //
                // short-cut for now, get the outerhtml, find the position, then remove the wrapping tags
                //
                if (string.IsNullOrEmpty(Key)) {
                    returnValue = textToInsert;
                } else {
                    returnValue = layout;
                    posStart = getTagStartPos( core, layout, 1, Key);
                    if (posStart != 0) {
                        posEnd = getTagEndPos( core, layout, posStart);
                        if (posEnd > 0) {
                            posStart = GenericController.strInstr(posStart + 1, layout, ">");
                            if (posStart != 0) {
                                posStart = posStart + 1;
                                posEnd = layout.LastIndexOf("<", posEnd - 2,StringComparison.InvariantCulture) + 1;
                                if (posEnd != 0) {
                                    returnValue = layout.left( posStart - 1) + textToInsert + layout.Substring(posEnd - 1);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnValue;
        }
        //
        //====================================================================================================
        /// <summary>
        /// getInnerHTML
        /// </summary>
        /// <param name="ignore"></param>
        /// <param name="layout"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string getInner(CoreController core, string layout, string key) {
            string returnValue = "";
            try {
                if ((string.IsNullOrEmpty(key)) || (string.IsNullOrEmpty(layout))) { return string.Empty; }
                returnValue = layout;
                int posStart = getTagStartPos(core, layout, 1, key);
                if (posStart != 0) {
                    int posEnd = getTagEndPos(core, layout, posStart);
                    if (posEnd > 0) {
                        posStart = GenericController.strInstr(posStart + 1, layout, ">");
                        if (posStart != 0) {
                            posStart = posStart + 1;
                            posEnd = layout.LastIndexOf("<", posEnd - 2,StringComparison.InvariantCulture) + 1;
                            if (posEnd != 0) {
                                //
                                // now move the end forward to skip trailing whitespace
                                //
                                do {
                                    posEnd = posEnd + 1;
                                } while ((posEnd < layout.Length) && (("\t\r\n\t ").IndexOf(layout.Substring(posEnd - 1, 1), StringComparison.InvariantCulture) != -1));
                                posEnd = posEnd - 1;
                                returnValue = layout.Substring(posStart - 1, (posEnd - posStart));
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnValue;
        }
        //
        //====================================================================================================
        /// <summary>
        /// getOuterHTML
        /// </summary>
        /// <param name="ignore"></param>
        /// <param name="layout"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string getOuter(CoreController core, string layout, string key) {
            string result = "";
            try {
                if ((string.IsNullOrEmpty(key)) || (string.IsNullOrEmpty(layout))) { return string.Empty; }
                string workingLayout = layout;
                int posStart = getTagStartPos(core, workingLayout, 1, key);
                if (posStart > 0) {
                    //
                    // now backtrack to include the leading whitespace
                    while ((posStart > 0) && (("\t\r\n\t ").IndexOf(workingLayout.Substring(posStart - 1, 1),StringComparison.InvariantCulture ) != -1)) {
                        posStart = posStart - 1;
                    }
                    workingLayout = workingLayout.Substring(posStart - 1);
                    int posEnd = getTagEndPos(core, workingLayout, 1);
                    if (posEnd > 0) {
                        workingLayout = workingLayout.left(posEnd - 1);
                        result = workingLayout;
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        private static bool tagMatch(CoreController core, string layout, int posStartTag, string searchId, string searchClass) {
            bool returnValue = false;
            try {
                const string attrAllowedChars = "abcdefghijklmnopqrstuvwzyz-_";
                string Tag = null;
                string tagLower = null;
                int Pos = 0;
                string Delimiter = null;
                string testChar = null;
                int tagLength = 0;
                int posValueStart = 0;
                string testValue = null;
                string[] testValues = null;
                int testCnt = 0;
                int Ptr = 0;
                //
                returnValue = false;
                Pos = GenericController.strInstr(posStartTag, layout, ">");
                if (Pos > 0) {
                    returnValue = true;
                    Tag = layout.Substring(posStartTag - 1, Pos - posStartTag + 1);
                    tagLower = GenericController.toLCase(Tag);
                    tagLength = Tag.Length;
                    //
                    // check searchId
                    //
                    if (returnValue && (!string.IsNullOrEmpty(searchId))) {
                        Pos = GenericController.strInstr(1, tagLower, " id=", 1);
                        if (Pos <= 0) {
                            //
                            // id required but this tag has no id attr
                            //
                            returnValue = false;
                        } else {
                            //
                            // test if the id attr value matches the searchClass
                            //
                            Pos = Pos + 4;
                            Delimiter = tagLower.Substring(Pos - 1, 1);
                            testValue = "";
                            if ((Delimiter == "\"") || (Delimiter == "'")) {
                                //
                                // search for end of delimited attribute value
                                //
                                posValueStart = Pos + 1;
                                do {
                                    Pos = Pos + 1;
                                    testChar = tagLower.Substring(Pos - 1, 1);
                                } while ((Pos < tagLength) && (testChar != Delimiter));
                                if (Pos >= tagLength) {
                                    //
                                    // delimiter not found, html error
                                    //
                                    returnValue = false;
                                } else {
                                    testValue = Tag.Substring(posValueStart - 1, Pos - posValueStart);
                                }
                            } else {
                                //
                                // search for end of non-delimited attribute value
                                //
                                posValueStart = Pos;
                                while ((Pos < tagLength) && (isInStr(1, attrAllowedChars, tagLower.Substring(Pos - 1, 1)))) {
                                    Pos = Pos + 1;
                                }
                                if (Pos >= tagLength) {
                                    //
                                    // delimiter not found, html error
                                    //
                                    returnValue = false;
                                } else {
                                    testValue = Tag.Substring(posValueStart - 1, Pos - posValueStart);
                                }
                            }
                            if (returnValue && (!string.IsNullOrEmpty(testValue))) {
                                //
                                //
                                //
                                if (searchId != testValue) {
                                    //
                                    // there can only be one id, and this does not match
                                    //
                                    returnValue = false;
                                }
                            }
                        }
                    }
                    //
                    // check searchClass
                    //
                    if (returnValue && (!string.IsNullOrEmpty(searchClass))) {
                        Pos = GenericController.strInstr(1, tagLower, " class=", 1);
                        if (Pos <= 0) {
                            //
                            // class required but this tag has no class attr
                            //
                            returnValue = false;
                        } else {
                            //
                            // test if the class attr value matches the searchClass
                            //
                            Pos = Pos + 7;
                            Delimiter = tagLower.Substring(Pos - 1, 1);
                            testValue = "";
                            if ((Delimiter == "\"") || (Delimiter == "'")) {
                                //
                                // search for end of delimited attribute value
                                //
                                posValueStart = Pos + 1;
                                do {
                                    Pos = Pos + 1;
                                    testChar = tagLower.Substring(Pos - 1, 1);
                                } while ((Pos < tagLength) && (testChar != Delimiter));
                                if (Pos >= tagLength) {
                                    //
                                    // delimiter not found, html error
                                    //
                                    returnValue = false;
                                } else {
                                    testValue = Tag.Substring(posValueStart - 1, Pos - posValueStart);
                                }
                            } else {
                                //
                                // search for end of non-delimited attribute value
                                //
                                posValueStart = Pos;
                                while ((Pos < tagLength) && (isInStr(1, attrAllowedChars, tagLower.Substring(Pos - 1, 1)))) {
                                    Pos = Pos + 1;
                                }
                                if (Pos >= tagLength) {
                                    //
                                    // delimiter not found, html error
                                    //
                                    returnValue = false;
                                } else {
                                    testValue = Tag.Substring(posValueStart - 1, Pos - posValueStart);
                                }
                            }
                            if (returnValue && (!string.IsNullOrEmpty(testValue))) {
                                //
                                //
                                //
                                testValues = testValue.Split(' ');
                                testCnt = testValues.GetUpperBound(0) + 1;
                                for (Ptr = 0; Ptr < testCnt; Ptr++) {
                                    if (searchClass == testValues[Ptr]) {
                                        break;
                                    }
                                }
                                if (Ptr >= testCnt) {
                                    returnValue = false;
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnValue;
        }
        //
        //====================================================================================================
        public static  int getTagStartPos(CoreController core, string layout, int layoutStartPos, string Key) {
            try {
                int returnPos = 0;
                int SegmentStart = 0;
                int Pos = 0;
                int LoopPtr = 0;
                string searchKey = null;
                int Ptr = 0;
                string workingKey = null;
                string[] workingKeys = null;
                string searchClass = null;
                string searchId = null;
                string searchTag = null;
                int posStartTag = 0;
                //
                returnPos = 0;
                workingKey = Key;
                if (GenericController.strInstr(1, workingKey, ">") != 0) {
                    //
                    // does not support > yet.
                    //
                    workingKey = GenericController.strReplace(workingKey, ">", " ");
                }
                //
                // eliminate whitespace
                //
                while (GenericController.strInstr(1, workingKey, "\t") != 0) {
                    workingKey = GenericController.strReplace(workingKey, "\t", " ");
                }
                //
                while (GenericController.strInstr(1, workingKey, "\r") != 0) {
                    workingKey = GenericController.strReplace(workingKey, "\r", " ");
                }
                //
                while (GenericController.strInstr(1, workingKey, "\n") != 0) {
                    workingKey = GenericController.strReplace(workingKey, "\n", " ");
                }
                //
                while (GenericController.strInstr(1, workingKey, "  ") != 0) {
                    workingKey = GenericController.strReplace(workingKey, "  ", " ");
                }
                //
                workingKey = workingKey.Trim(' ');
                //
                if (GenericController.strInstr(1, workingKey, " ") != 0) {
                    //
                    // if there are spaces, do them sequentially
                    //
                    workingKeys = workingKey.Split(' ');
                    SegmentStart = 1;
                    while ((!string.IsNullOrEmpty(layout)) && (SegmentStart != 0) && (Ptr <= workingKeys.GetUpperBound(0))) {
                        SegmentStart = getTagStartPos(null, layout, SegmentStart, workingKeys[Ptr]);
                        Ptr = Ptr + 1;
                    }
                    returnPos = SegmentStart;
                } else {
                    //
                    // find this single key and get the outerHTML
                    //   at this point, the key can be
                    //       a class = .xxxx
                    //       an id = #xxxx
                    //       a tag = xxxx
                    //       a compound in either form, xxxx.xxxx or xxxx#xxxx
                    //
                    //   searchKey = the search pattern to start
                    //
                    if (workingKey.left( 1) == ".") {
                        //
                        // search for a class
                        //
                        searchClass = workingKey.Substring(1);
                        searchTag = "";
                        searchId = "";
                        Pos = GenericController.strInstr(1, searchClass, "#");
                        if (Pos != 0) {
                            searchId = searchClass.Substring(Pos - 1);
                            searchClass = searchClass.left( Pos - 1);
                        }
                        //
                        searchKey = "<";
                    } else if (workingKey.left( 1) == "#") {
                        //
                        // search for an ID
                        //
                        searchClass = "";
                        searchTag = "";
                        searchId = workingKey.Substring(1);
                        Pos = GenericController.strInstr(1, searchId, ".");
                        if (Pos != 0) {
                            searchClass = searchId.Substring(Pos - 1);
                            searchId = searchId.left( Pos - 1);
                        }
                        //
                        searchKey = "<";
                    } else {
                        //
                        // search for a tagname
                        //
                        searchClass = "";
                        searchTag = workingKey;
                        searchId = "";
                        //
                        Pos = GenericController.strInstr(1, searchTag, "#");
                        if (Pos != 0) {
                            searchId = searchTag.Substring(Pos);
                            searchTag = searchTag.left( Pos - 1);
                            Pos = GenericController.strInstr(1, searchId, ".");
                            if (Pos != 0) {
                                searchClass = searchId.Substring(Pos - 1);
                                searchId = searchId.left( Pos - 1);
                            }
                        }
                        Pos = GenericController.strInstr(1, searchTag, ".");
                        if (Pos != 0) {
                            searchClass = searchTag.Substring(Pos);
                            searchTag = searchTag.left( Pos - 1);
                            Pos = GenericController.strInstr(1, searchClass, "#");
                            if (Pos != 0) {
                                searchId = searchClass.Substring(Pos - 1);
                                searchClass = searchClass.left( Pos - 1);
                            }
                        }
                        //
                        searchKey = "<" + searchTag;
                    }
                    Pos = layoutStartPos;
                    do {
                        Pos = GenericController.strInstr(Pos, layout, searchKey);
                        if (Pos == 0) {
                            //
                            // not found, return empty
                            //
                            break;
                        } else {
                            //
                            // string found - go to the start of the tag
                            //
                            posStartTag = layout.LastIndexOf("<", Pos, StringComparison.InvariantCulture) + 1;
                            if (posStartTag <= 0) {
                                //
                                // bad html, no start tag found
                                //
                                Pos = 0;
                                returnPos = 0;
                            } else if (layout.Substring(posStartTag - 1, 2) == "</") {
                                //
                                // this is an end tag, skip it
                                //
                                Pos = Pos + 1;
                            } else if (tagMatch( core, layout, posStartTag, searchId, searchClass)) {
                                //
                                // match, return with this position
                                //
                                returnPos = Pos;
                                break;
                            } else {
                                //
                                // no match, skip this and go to the next
                                //
                                Pos = Pos + 1;
                            }
                        }
                        LoopPtr = LoopPtr + 1;
                    } while (LoopPtr < 1000);
                    //
                    //
                    //
                    if (LoopPtr >= 10000) {
                        logger.Error($"{core.logCommonMessage}",new GenericException("Tag limit of 10000 tags per block reached."));
                    }
                }
                //
                return returnPos;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //=================================================================================================
        //   return the position following the tag which closes the tag that starts the string
        //       starting postion<div><div><p>this and that</p><!-- </div> --></div></div>And a whole lot more
        //       returns the position of the "A" following the last /div
        //       string 123<img>456 returns pointing to "4"
        //       string 123456 returns pointing to "6"
        //       returns 0 if the end was not found
        //=================================================================================================
        //
        public static int getTagEndPos(CoreController core, string Source, int startPos) {
            int returnValue = 0;
            try {
                int Pos = 0;
                string TagName = null;
                string endTag = null;
                string startTag = null;
                int posNest = 0;
                int posEnd = 0;
                int posComment = 0;
                string c = null;
                //
                Pos = GenericController.strInstr(startPos, Source, "<");
                TagName = "";
                returnValue = 0;
                if (Pos != 0) {
                    Pos = Pos + 1;
                    while (Pos < Source.Length) {
                        c = GenericController.toLCase(Source.Substring(Pos - 1, 1));
                        if ((string.CompareOrdinal(c, "a") >= 0) && (string.CompareOrdinal(c, "z") <= 0)) {
                            TagName = TagName + c;
                        } else {
                            break;
                        }
                        Pos = Pos + 1;
                    }
                    if (!string.IsNullOrEmpty(TagName)) {
                        endTag = "</" + TagName;
                        startTag = "<" + TagName;
                        while (Pos != 0) {
                            posEnd = GenericController.strInstr(Pos + 1, Source, endTag, 1);
                            if (posEnd == 0) {
                                //
                                // no end was found, return the tag or rest of the string
                                //
                                returnValue = GenericController.strInstr(Pos + 1, Source, ">") + 1;
                                if (posEnd == 1) {
                                    returnValue = Source.Length;
                                }
                                break;
                            } else {
                                posNest = GenericController.strInstr(Pos + 1, Source, startTag, 1);
                                if (posNest == 0) {
                                    //
                                    // no nest found, set to end
                                    //
                                    posNest = Source.Length;
                                }
                                posComment = GenericController.strInstr(Pos + 1, Source, "<!--");
                                if (posComment == 0) {
                                    //
                                    // no comment found, set to end
                                    //
                                    posComment = Source.Length;
                                }
                                if ((posNest < posEnd) && (posNest < posComment)) {
                                    //
                                    // ----- the tag is nested, find the end of the nest
                                    //
                                    Pos = getTagEndPos(core, Source, posNest);
                                    // 8/28/2012, if there is a nested tag right before the correct end tag, it skips the end:
                                    // <div class=a>a<div class=b>b</div></div>
                                    // the second /div is missed because returnValue returns one past the >, then the
                                    // next search starts +1 that position
                                    if (Pos > 0) {
                                        Pos = Pos - 1;
                                    }
                                } else if (posComment < posEnd) {
                                    //
                                    // ----- there is a comment between the tag and the first tagend, skip it
                                    //
                                    Pos = GenericController.strInstr(posComment, Source, "-->");
                                    if (Pos == 0) {
                                        //
                                        // start comment with no end, exit now
                                        //
                                        returnValue = Source.Length;
                                        break;
                                    }
                                } else {
                                    //
                                    // ----- end position is here, go to the end of it and exit
                                    //
                                    Pos = GenericController.strInstr(posEnd, Source, ">");
                                    if (Pos == 0) {
                                        //
                                        // no end was found, just exit
                                        //
                                        returnValue = Source.Length;
                                        break;
                                    } else {
                                        //
                                        // ----- end was found
                                        //
                                        returnValue = Pos + 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return returnValue;
        }
        //
        //========================================================================================================
        //
        // Finds all tags matching the input, and concatinates them into the output
        // does NOT account for nested tags, use for body, script, style
        //
        // ReturnAll - if true, it returns all the occurances, back-to-back
        //
        //========================================================================================================
        //
        public static string getTagInnerHTML(string PageSource, string Tag, bool ReturnAll) {
            string result = "";
            try {
                //
                int TagStart = 0;
                int TagEnd = 0;
                int LoopCnt = 0;
                int Pos = 0;
                int CommentPos = 0;
                int ScriptPos = 0;
                //
                result = "";
                Pos = 1;
                while ((Pos > 0) && (LoopCnt < 100)) {
                    TagStart = GenericController.strInstr(Pos, PageSource, "<" + Tag, 1);
                    if (TagStart == 0) {
                        Pos = 0;
                    } else {
                        //
                        // tag found, skip any comments that start between current position and the tag
                        //
                        CommentPos = GenericController.strInstr(Pos, PageSource, "<!--");
                        if ((CommentPos != 0) && (CommentPos < TagStart)) {
                            //
                            // skip comment and start again
                            //
                            Pos = GenericController.strInstr(CommentPos, PageSource, "-->");
                        } else {
                            ScriptPos = GenericController.strInstr(Pos, PageSource, "<script");
                            if ((ScriptPos != 0) && (ScriptPos < TagStart)) {
                                //
                                // skip comment and start again
                                //
                                Pos = GenericController.strInstr(ScriptPos, PageSource, "</script");
                            } else {
                                //
                                // Get the tags innerHTML
                                //
                                TagStart = GenericController.strInstr(TagStart, PageSource, ">", 1);
                                Pos = TagStart;
                                if (TagStart != 0) {
                                    TagStart = TagStart + 1;
                                    TagEnd = GenericController.strInstr(TagStart, PageSource, "</" + Tag, 1);
                                    if (TagEnd != 0) {
                                        result += PageSource.Substring(TagStart - 1, TagEnd - TagStart);
                                    }
                                }
                            }
                        }
                        LoopCnt = LoopCnt + 1;
                        if (ReturnAll) {
                            TagStart = GenericController.strInstr(TagEnd, PageSource, "<" + Tag, 1);
                        } else {
                            TagStart = 0;
                        }
                    }
                }
            } catch   {
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
