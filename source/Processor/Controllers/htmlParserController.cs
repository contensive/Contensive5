
using System;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Controllers {
    public class HtmlParserController {
        //
        //====================================================================================================
        // open source html parser to try
        // ************* NUGET html parser
        //
        //
        //
        //
        // Parse HTML
        //
        //   This class parses an HTML document into Nodes. A node may be text, and it
        //   may be a tag. Use the IsTag method to detect
        //
        //   It makes no attempt to create a document structer. The advantage is that
        //   it can parse through, and make available poor HTML structures
        //
        //   If Element.IsTag
        //       Element.text = the tag, including <>
        //   otherwise
        //       Element.text = the string
        //============================================================================
        //
        //   Internal Storage
        //
        private readonly CoreController core;
        //
        private const bool NewWay = true;
        //
        private Element[] LocalElements;
        private int LocalElementSize;
        private string[] SplitStore;
        private int SplitStoreCnt;
        private string[] Blobs;
        private int BlobCnt;
        private string BlobSN;
        //
        //   Internal HTML Element Attribute structure
        //
        private struct ElementAttributeStructure {
            public string Name;
            public string UcaseName;
            public string Value;
        }
        //
        //   Internal HTML Element (tag) structure
        //
        private struct Element {
            public bool IsTag;
            public string TagName;
            public string Text;
            public int Position;
            public int AttributeCount;
            public int AttributeSize;
            public ElementAttributeStructure[] Attributes;
            public bool Loaded;
        }
        //
        //====================================================================================================
        //
        public HtmlParserController(CoreController core) {
            this.core = core;
        }
        //
        //====================================================================================================
        //   Parses the string, returns true if loaded OK
        //
        public bool load(string HTMLSource) {
            bool tempLoad = false;
            try {
                //
                string WorkingSrc = null;
                string[] splittest = null;
                int Ptr = 0;
                int Cnt = 0;
                int PosScriptEnd = 0;
                int PosEndScript = 0;
                //
                // ----- initialize internal storage
                //
                WorkingSrc = HTMLSource;
                elementCount = 0;
                LocalElementSize = 0;
                LocalElements = new Contensive.Processor.Controllers.HtmlParserController.Element[LocalElementSize + 1];
                tempLoad = true;
                Ptr = 0;
                //
                // get a unique signature
                //
                do {
                    BlobSN = "/blob" + encodeText(GenericController.getRandomInteger()) + ":";
                    Ptr = Ptr + 1;
                } while ((WorkingSrc.IndexOf(BlobSN, System.StringComparison.OrdinalIgnoreCase) != -1) && (Ptr < 10));
                //
                // remove all scripting
                //
                splittest = WorkingSrc.Split(new[] { "<script" }, StringSplitOptions.None);
                //  Regex.Split( WorkingSrc, "<script");
                Cnt = splittest.GetUpperBound(0) + 1;
                if (Cnt > 1) {
                    for (Ptr = 1; Ptr < Cnt; Ptr++) {
                        PosScriptEnd = GenericController.strInstr(1, splittest[Ptr], ">");
                        if (PosScriptEnd > 0) {
                            PosEndScript = GenericController.strInstr(PosScriptEnd, splittest[Ptr], "</script");
                            if (PosEndScript > 0) {
                                Array.Resize(ref Blobs, BlobCnt + 1);
                                Blobs[BlobCnt] = splittest[Ptr].Substring(PosScriptEnd, (PosEndScript - 1) - (PosScriptEnd + 1) + 1);
                                splittest[Ptr] = splittest[Ptr].left(PosScriptEnd) + BlobSN + BlobCnt + "/" + splittest[Ptr].Substring(PosEndScript - 1);
                                BlobCnt = BlobCnt + 1;
                            }
                        }
                    }
                    WorkingSrc = string.Join("<script", splittest);
                }
                //
                // remove all styles
                //
                splittest = GenericController.stringSplit(WorkingSrc, "<style");
                Cnt = splittest.GetUpperBound(0) + 1;
                if (Cnt > 1) {
                    for (Ptr = 1; Ptr < Cnt; Ptr++) {
                        PosScriptEnd = GenericController.strInstr(1, splittest[Ptr], ">");
                        if (PosScriptEnd > 0) {
                            PosEndScript = GenericController.strInstr(PosScriptEnd, splittest[Ptr], "</style", 1);
                            if (PosEndScript > 0) {
                                Array.Resize(ref Blobs, BlobCnt + 1);
                                Blobs[BlobCnt] = splittest[Ptr].Substring(PosScriptEnd, (PosEndScript - 1) - (PosScriptEnd + 1) + 1);
                                splittest[Ptr] = splittest[Ptr].left(PosScriptEnd) + BlobSN + BlobCnt + "/" + splittest[Ptr].Substring(PosEndScript - 1);
                                BlobCnt = BlobCnt + 1;
                            }
                        }
                    }
                    WorkingSrc = string.Join("<style", splittest);
                }
                //
                // remove comments
                //
                splittest = GenericController.stringSplit(WorkingSrc, "<!--");
                Cnt = splittest.GetUpperBound(0) + 1;
                if (Cnt > 1) {
                    for (Ptr = 1; Ptr < Cnt; Ptr++) {
                        PosScriptEnd = GenericController.strInstr(1, splittest[Ptr], "-->");
                        if (PosScriptEnd > 0) {
                            Array.Resize(ref Blobs, BlobCnt + 1);
                            Blobs[BlobCnt] = splittest[Ptr].left(PosScriptEnd - 1);
                            splittest[Ptr] = BlobSN + BlobCnt + "/" + splittest[Ptr].Substring(PosScriptEnd - 1);
                            BlobCnt = BlobCnt + 1;
                        }
                    }
                    WorkingSrc = string.Join("<!--", splittest);
                }
                //
                // Split the html on <
                //
                SplitStore = WorkingSrc.Split('<');
                SplitStoreCnt = SplitStore.GetUpperBound(0) + 1;
                elementCount = (SplitStoreCnt * 2);
                LocalElements = new Contensive.Processor.Controllers.HtmlParserController.Element[elementCount + 1];
                return tempLoad;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempLoad;
        }
        //
        //====================================================================================================
        //   Get the element count
        //
        public int elementCount { get; private set; }
        //
        //====================================================================================================
        //   is the specified element a tag (or text)
        //
        public bool isTag(int ElementPointer) {
            bool result = false;
            try {
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    result = LocalElements[ElementPointer].IsTag;
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
        //====================================================================================================
        //   Get the LocalElements value
        //
        public string text(int ElementPointer) {
            string tempText = null;
            try {
                //
                tempText = "";
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    tempText = LocalElements[ElementPointer].Text;
                }
                //
                return tempText;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempText;
        }
        //
        //====================================================================================================
        //   Get the LocalElements value
        //
        public string tagName(int ElementPointer) {
            string tempTagName = null;
            try {
                //
                tempTagName = "";
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    tempTagName = LocalElements[ElementPointer].TagName;
                }
                //
                return tempTagName;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempTagName;
        }
        //
        //====================================================================================================
        //   Get the LocalElements value
        //
        public int position(int ElementPointer) {
            int tempPosition = 0;
            try {
                //
                tempPosition = 0;
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    tempPosition = LocalElements[ElementPointer].Position;
                }
                //
                return tempPosition;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempPosition;
        }
        //
        //====================================================================================================
        //   Get an LocalElements attribute count
        //
        public int elementAttributeCount(int ElementPointer) {
            int tempElementAttributeCount = 0;
            try {
                //
                tempElementAttributeCount = 0;
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    tempElementAttributeCount = LocalElements[ElementPointer].AttributeCount;
                }
                //
                return tempElementAttributeCount;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempElementAttributeCount;
        }
        //
        //====================================================================================================
        //   Get an LocalElements attribute name
        //
        public string elementAttributeName(int ElementPointer, int AttributePointer) {
            string tempElementAttributeName = null;
            try {
                //
                tempElementAttributeName = "";
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    if (AttributePointer < LocalElements[ElementPointer].AttributeCount) {
                        tempElementAttributeName = LocalElements[ElementPointer].Attributes[AttributePointer].Name;
                    }
                }
                //
                return tempElementAttributeName;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempElementAttributeName;
        }
        //
        //====================================================================================================
        //   Get an LocalElements attribute value
        //
        public string elementAttributeValue(int ElementPointer, int AttributePointer) {
            string tempElementAttributeValue = null;
            try {
                //
                tempElementAttributeValue = "";
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    if (AttributePointer < LocalElements[ElementPointer].AttributeCount) {
                        tempElementAttributeValue = LocalElements[ElementPointer].Attributes[AttributePointer].Value;
                    }
                }
                //
                return tempElementAttributeValue;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempElementAttributeValue;
        }
        //
        //====================================================================================================
        //   Get an LocalElements attribute value
        //
        public string elementAttribute(int ElementPointer, string Name) {
            string tempElementAttribute = null;
            try {
                //
                int AttributePointer = 0;
                string UcaseName = null;
                //
                tempElementAttribute = "";
                LoadElement(ElementPointer);
                if (ElementPointer < elementCount) {
                    if (LocalElements[ElementPointer].AttributeCount > 0) {
                        UcaseName = GenericController.toUCase(Name);
                        int tempVar = LocalElements[ElementPointer].AttributeCount;
                        for (AttributePointer = 0; AttributePointer < tempVar; AttributePointer++) {
                            if (LocalElements[ElementPointer].Attributes[AttributePointer].UcaseName == UcaseName) {
                                tempElementAttribute = LocalElements[ElementPointer].Attributes[AttributePointer].Value;
                                break;
                            }
                        }
                    }
                }
                //
                return tempElementAttribute;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempElementAttribute;
        }
        //
        //====================================================================================================
        //   Parse a Tag element into its attributes
        //
        private void ParseTag(int ElementPointer) {
            try {
                string TagString = null;
                string[] AttrSplit = null;
                int AttrCount = 0;
                int AttrPointer = 0;
                string AttrName = null;
                string AttrValue = null;
                int AttrValueLen = 0;
                //
                TagString = LocalElements[ElementPointer].Text.Substring(1, LocalElements[ElementPointer].Text.Length - 2);
                if (!string.IsNullOrEmpty(TagString) && (TagString.Substring(TagString.Length - 1) == "/")) {
                    TagString = TagString.left(TagString.Length - 1);
                }
                TagString = GenericController.strReplace(TagString, "\r", " ");
                TagString = GenericController.strReplace(TagString, "\n", " ");
                TagString = GenericController.strReplace(TagString, "  ", " ");
                LocalElements[ElementPointer].AttributeCount = 0;
                LocalElements[ElementPointer].AttributeSize = 1;
                LocalElements[ElementPointer].Attributes = new Contensive.Processor.Controllers.HtmlParserController.ElementAttributeStructure[1];
                if (!string.IsNullOrEmpty(TagString)) {
                    AttrSplit = splitDelimited(TagString, " ");
                    AttrCount = AttrSplit.GetUpperBound(0) + 1;
                    if (AttrCount > 0) {
                        LocalElements[ElementPointer].TagName = AttrSplit[0];
                        if (LocalElements[ElementPointer].TagName == "!--") {
                            //
                            // Skip comment tags, ignore the attributes
                            //
                        } else {
                            //
                            // Process the tag
                            //
                            if (AttrCount > 1) {
                                for (AttrPointer = 1; AttrPointer < AttrCount; AttrPointer++) {
                                    AttrName = AttrSplit[AttrPointer];
                                    if (!string.IsNullOrEmpty(AttrName)) {
                                        if (LocalElements[ElementPointer].AttributeCount >= LocalElements[ElementPointer].AttributeSize) {
                                            LocalElements[ElementPointer].AttributeSize = LocalElements[ElementPointer].AttributeSize + 5;
                                            Array.Resize(ref LocalElements[ElementPointer].Attributes, (LocalElements[ElementPointer].AttributeSize) + 1);
                                        }
                                        int EqualPosition = GenericController.strInstr(1, AttrName, "=");
                                        if (EqualPosition == 0) {
                                            LocalElements[ElementPointer].Attributes[LocalElements[ElementPointer].AttributeCount].Name = AttrName;
                                            LocalElements[ElementPointer].Attributes[LocalElements[ElementPointer].AttributeCount].UcaseName = GenericController.toUCase(AttrName);
                                            LocalElements[ElementPointer].Attributes[LocalElements[ElementPointer].AttributeCount].Value = AttrName;
                                        } else {
                                            AttrValue = AttrName.Substring(EqualPosition);
                                            AttrValueLen = AttrValue.Length;
                                            if (AttrValueLen > 1) {
                                                if ((AttrValue.left(1) == "\"") && (AttrValue.Substring(AttrValueLen - 1, 1) == "\"")) {
                                                    AttrValue = AttrValue.Substring(1, AttrValueLen - 2);
                                                }
                                            }
                                            AttrName = AttrName.left(EqualPosition - 1);
                                            LocalElements[ElementPointer].Attributes[LocalElements[ElementPointer].AttributeCount].Name = AttrName;
                                            LocalElements[ElementPointer].Attributes[LocalElements[ElementPointer].AttributeCount].UcaseName = GenericController.toUCase(AttrName);
                                            LocalElements[ElementPointer].Attributes[LocalElements[ElementPointer].AttributeCount].Value = AttrValue;
                                        }
                                        LocalElements[ElementPointer].AttributeCount = LocalElements[ElementPointer].AttributeCount + 1;
                                    }
                                }
                            }
                        }
                    }
                }
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        //
        private int GetLesserNonZero(int value0, int value1) {
            int tempGetLesserNonZero = 0;
            try {
                //
                if (value0 == 0) {
                    tempGetLesserNonZero = value1;
                } else {
                    if (value1 == 0) {
                        tempGetLesserNonZero = value0;
                    } else {
                        if (value0 < value1) {
                            tempGetLesserNonZero = value0;
                        } else {
                            tempGetLesserNonZero = value1;
                        }
                    }
                }
                //
                return tempGetLesserNonZero;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempGetLesserNonZero;
        }
        //
        //====================================================================================================
        // Pass spaces at the current cursor position
        //
        private int PassWhiteSpace(int CursorPosition, string TagString) {
            int tempPassWhiteSpace = 0;
            try {
                //
                tempPassWhiteSpace = CursorPosition;
                while ((TagString.Substring(tempPassWhiteSpace - 1, 1) == " ") && (tempPassWhiteSpace < TagString.Length)) {
                    tempPassWhiteSpace = tempPassWhiteSpace + 1;
                }
                //
                return tempPassWhiteSpace;
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return tempPassWhiteSpace;
        }
        //
        //====================================================================================================
        //
        private void LoadElement(int ElementPtr) {
            int SplitPtr = 0;
            string SplitSrc = null;
            int ElementBasePtr = 0;
            int Ptr = 0;
            string SrcTag = null;
            string SrcBody = null;
            //
            if (NewWay) {
                if (!(LocalElements[ElementPtr].Loaded)) {
                    SplitPtr = encodeInteger(ElementPtr / 2.0);
                    ElementBasePtr = SplitPtr * 2;
                    SplitSrc = SplitStore[SplitPtr];
                    Ptr = GenericController.strInstr(1, SplitSrc, ">");
                    //
                    // replace blobs
                    //
                    if (Ptr == 0) {
                        SrcTag = "";
                        SrcBody = ReplaceBlob(SplitSrc);
                    } else {
                        SrcTag = ReplaceBlob(SplitSrc.left(Ptr));
                        SrcBody = ReplaceBlob(SplitSrc.Substring(Ptr));
                    }
                    if (Ptr == 0) {
                        if (ElementPtr == 0) {
                            //
                            // no close tag, elementptr=0 then First entry is empty, second is body
                            //
                            LocalElements[ElementBasePtr].AttributeCount = 0;
                            LocalElements[ElementBasePtr].IsTag = false;
                            LocalElements[ElementBasePtr].Loaded = true;
                            LocalElements[ElementBasePtr].Position = 0;
                            LocalElements[ElementBasePtr].Text = "";
                            //
                            LocalElements[ElementBasePtr + 1].AttributeCount = 0;
                            LocalElements[ElementBasePtr + 1].IsTag = false;
                            LocalElements[ElementBasePtr + 1].Loaded = true;
                            LocalElements[ElementBasePtr + 1].Position = 0;
                            LocalElements[ElementBasePtr + 1].Text = SplitSrc;
                        } else {
                            //
                            // no close tag, elementptr>0 then First entry is '<', second is body
                            //
                            LocalElements[ElementBasePtr].AttributeCount = 0;
                            LocalElements[ElementBasePtr].IsTag = false;
                            LocalElements[ElementBasePtr].Loaded = true;
                            LocalElements[ElementBasePtr].Position = 0;
                            LocalElements[ElementBasePtr].Text = "<";
                            //
                            LocalElements[ElementBasePtr + 1].AttributeCount = 0;
                            LocalElements[ElementBasePtr + 1].IsTag = false;
                            LocalElements[ElementBasePtr + 1].Loaded = true;
                            LocalElements[ElementBasePtr + 1].Position = 0;
                            LocalElements[ElementBasePtr + 1].Text = SplitSrc;
                        }
                    } else {
                        //
                        // close tag found, first entry is tag text, second entry is body
                        //
                        LocalElements[ElementBasePtr].Text = "<" + SrcTag;
                        LocalElements[ElementBasePtr].IsTag = true;
                        ParseTag(ElementBasePtr);
                        LocalElements[ElementBasePtr].Loaded = true;
                        //
                        LocalElements[ElementBasePtr + 1].AttributeCount = 0;
                        LocalElements[ElementBasePtr + 1].IsTag = false;
                        LocalElements[ElementBasePtr + 1].Loaded = true;
                        LocalElements[ElementBasePtr + 1].Position = 0;
                        LocalElements[ElementBasePtr + 1].Text = SrcBody;
                    }
                }
            }
        }
        //
        //====================================================================================================
        //
        private string ReplaceBlob(string Src) {
            string tempReplaceBlob = null;
            int Pos = 0;
            int PosEnd = 0;
            int PosNum = 0;
            string PtrText = null;
            int Ptr = 0;
            string Blob = "";
            //
            tempReplaceBlob = Src;
            Pos = GenericController.strInstr(1, Src, BlobSN);
            if (Pos != 0) {
                PosEnd = GenericController.strInstr(Pos + 1, Src, "/");
                if (PosEnd > 0) {
                    PosNum = GenericController.strInstr(Pos + 1, Src, ":");
                    if (PosNum > 0) {
                        PtrText = Src.Substring(PosNum, PosEnd - PosNum - 1);
                        if (PtrText.isNumeric()) {
                            Ptr = int.Parse(PtrText);
                            if (Ptr < BlobCnt) {
                                Blob = Blobs[Ptr];
                            }
                            tempReplaceBlob = Src.left(Pos - 1) + Blob + Src.Substring(PosEnd);
                        }
                    }
                }
            }

            return tempReplaceBlob;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
