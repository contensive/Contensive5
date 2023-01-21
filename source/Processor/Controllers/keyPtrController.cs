
using System;
using static Contensive.Processor.Controllers.GenericController;
using static Newtonsoft.Json.JsonConvert;

namespace Contensive.Processor.Controllers {
    //
    public class KeyPtrController {
        //
        // new serializable and deserialize
        //   declare a private instance of a class that holds everything
        //   keyPtrIndex uses the class
        //   call serialize on keyPtrIndex to json serialize the storage object and return the string
        //   call deserialise to populate the storage object from the argument
        //
        // ----- Index Type - This structure is the basis for Element Indexing
        //       Records are read into thier data structure, and keys(Key,ID,etc.) and pointers
        //       are put in the KeyPointerArrays.
        //           AddIndex( Key, value )
        //           BubbleSort( Index ) - sorts the index by the key field
        //           GetIndexValue( index, Key ) - retrieves the pointer
        //
        // These  GUIDs provide the COM identity for this class 
        // and its COM interfaces. If you change them, existing 
        // clients will no longer be able to access the class.
        // -Public Const ClassId As String = "BB8AFA32-1C0A-4CDB-BE3B-D9E6AA91A656"
        // -Public Const InterfaceId As String = "353333D8-FB3B-4340-B8B6-C5547B46F5DF"
        // -Public Const EventsId As String = "1407C7AD-08DF-44DB-898E-7B3CB9F86EB3"
        //
        private const int KeyPointerArrayChunk = 1000;
        //
        public class StorageClass {
            //
            public int ArraySize;
            public int ArrayCount;
            public bool ArrayDirty;
            public string[] UcaseKeyArray;
            public string[] PointerArray;
            public int ArrayPointer;
        }
        //
        private StorageClass store = new StorageClass();
        //
        //
        //
        public string exportPropertyBag()  {
            string returnBag = "";
            try {
                returnBag = SerializeObject(store);
            } catch (Exception ex) {
                throw new IndexException("ExportPropertyBag error", ex);
            }
            return returnBag;
        }
        //
        //
        //
        public void importPropertyBag(string bag) {
            try {
                store = DeserializeObject<StorageClass>(bag);
            } catch (Exception ex) {
                throw new IndexException("ImportPropertyBag error", ex);
            }
        }
        //
        //========================================================================
        //   Returns a pointer into the index for this Key
        //   Used only by GetIndexValue and setIndexValue
        //   Returns -1 if there is no match
        //========================================================================
        //
        private int GetArrayPointer(string Key) {
            int ArrayPointer = -1;
            try {
                string UcaseTargetKey = null;
                int HighGuess = 0;
                int LowGuess = 0;
                int PointerGuess = 0;
                //
                if (store.ArrayDirty) {
                    Sort();
                }
                //
                ArrayPointer = -1;
                if (store.ArrayCount > 0) {
                    UcaseTargetKey = GenericController.strReplace(Key.ToUpper(), Environment.NewLine, "");
                    LowGuess = -1;
                    HighGuess = store.ArrayCount - 1;
                    while ((HighGuess - LowGuess) > 1) {
                        PointerGuess = encodeInteger(Math.Floor((HighGuess + LowGuess) / 2.0));
                        if (UcaseTargetKey == store.UcaseKeyArray[PointerGuess]) {
                            HighGuess = PointerGuess;
                            break;
                        } else if (string.CompareOrdinal(UcaseTargetKey, store.UcaseKeyArray[PointerGuess]) < 0) {
                            HighGuess = PointerGuess;
                        } else {
                            LowGuess = PointerGuess;
                        }
                    }
                    if (UcaseTargetKey == store.UcaseKeyArray[HighGuess]) {
                        ArrayPointer = HighGuess;
                    }
                }

            } catch (Exception ex) {
                throw new IndexException("getArrayPointer error", ex);
            }
            return ArrayPointer;
        }
        //
        //========================================================================
        //   Returns the matching pointer from a ContentIndex
        //   Returns -1 if there is no match
        //========================================================================
        //
        public int getPtr(string Key) {
            int returnKey = -1;
            try {
                bool MatchFound = false;
                string UcaseKey = null;
                //
                UcaseKey = GenericController.strReplace(Key.ToUpper(), Environment.NewLine, "");
                store.ArrayPointer = GetArrayPointer(Key);
                if (store.ArrayPointer > -1) {
                    MatchFound = true;
                    while (MatchFound) {
                        store.ArrayPointer = store.ArrayPointer - 1;
                        if (store.ArrayPointer < 0) {
                            MatchFound = false;
                        } else {
                            MatchFound = (store.UcaseKeyArray[store.ArrayPointer] == UcaseKey);
                        }
                    }
                    store.ArrayPointer = store.ArrayPointer + 1;
                    returnKey = GenericController.encodeInteger(store.PointerArray[store.ArrayPointer]);
                }
            } catch (Exception ex) {
                throw new IndexException("GetPointer error", ex);
            }
            return returnKey;
        }
        //
        //========================================================================
        //   Add an element to an ContentIndex
        //
        //   if the entry is a duplicate, it is added anyway
        //========================================================================
        //
        public void setPtr(string Key, int Pointer) {
            try {
                string keyToSave;
                //
                keyToSave = GenericController.strReplace(Key.ToUpper(), Environment.NewLine, "");
                //
                if (store.ArrayCount >= store.ArraySize) {
                    store.ArraySize = store.ArraySize + KeyPointerArrayChunk;
                    Array.Resize(ref store.PointerArray, store.ArraySize + 1);
                    Array.Resize(ref store.UcaseKeyArray, store.ArraySize + 1);
                }
                store.ArrayPointer = store.ArrayCount;
                store.ArrayCount = store.ArrayCount + 1;
                store.UcaseKeyArray[store.ArrayPointer] = keyToSave;
                store.PointerArray[store.ArrayPointer] = Pointer.ToString();
                store.ArrayDirty = true;
            } catch (Exception ex) {
                throw new IndexException("SetPointer error", ex);
            }
        }
        //
        //========================================================================
        //   Returns the next matching pointer from a ContentIndex
        //   Returns -1 if there is no match
        //========================================================================
        //
        public int getNextPtrMatch(string Key) {
            int nextPointerMatch = -1;
            try {
                string UcaseKey = null;
                //
                if (store.ArrayPointer < (store.ArrayCount - 1)) {
                    store.ArrayPointer = store.ArrayPointer + 1;
                    UcaseKey = GenericController.toUCase(Key);
                    if (store.UcaseKeyArray[store.ArrayPointer] == UcaseKey) {
                        nextPointerMatch = GenericController.encodeInteger(store.PointerArray[store.ArrayPointer]);
                    } else {
                        store.ArrayPointer = store.ArrayPointer - 1;
                    }
                }
            } catch (Exception ex) {
                throw new IndexException("GetNextPointerMatch error", ex);
            }
            return nextPointerMatch;
        }
        //
        //========================================================================
        //   Returns the first Pointer in the current index
        //   returns empty if there are no Pointers indexed
        //========================================================================
        //
        public int getFirstPtr()  {
            int firstPointer = -1;
            try {
                if (store.ArrayDirty) {
                    Sort();
                }
                //
                // GetFirstPointer = -1
                if (store.ArrayCount > 0) {
                    store.ArrayPointer = 0;
                    firstPointer = GenericController.encodeInteger(store.PointerArray[store.ArrayPointer]);
                }
                //
            } catch (Exception ex) {
                throw new IndexException("GetFirstPointer error", ex);
            }
            return firstPointer;
        }
        //
        //========================================================================
        //   Returns the next Pointer, past the last one returned
        //   Returns empty if the index is at the end
        //========================================================================
        //
        public int getNextPtr()  {
            int nextPointer = -1;
            try {
                if (store.ArrayDirty) {
                    Sort();
                }
                //
                if ((store.ArrayPointer + 1) < store.ArrayCount) {
                    store.ArrayPointer = store.ArrayPointer + 1;
                    nextPointer = GenericController.encodeInteger(store.PointerArray[store.ArrayPointer]);
                }
            } catch (Exception ex) {
                throw new IndexException("GetPointer error", ex);
            }
            return nextPointer;
        }
        //
        //========================================================================
        //
        //========================================================================
        //
        private void BubbleSort()  {
            try {
                string TempUcaseKey = null;
                string tempPtrString = null;
                bool CleanPass = false;
                int MaxPointer = 0;
                int SlowPointer = 0;
                int FastPointer = 0;
                int PointerDelta = 0;
                //
                if (store.ArrayCount > 1) {
                    PointerDelta = 1;
                    MaxPointer = store.ArrayCount - 2;
                    for (SlowPointer = MaxPointer; SlowPointer >= 0; SlowPointer--) {
                        CleanPass = true;
                        int tempVar = MaxPointer - SlowPointer;
                        for (FastPointer = MaxPointer; FastPointer >= tempVar; FastPointer--) {
                            if (string.CompareOrdinal(store.UcaseKeyArray[FastPointer], store.UcaseKeyArray[FastPointer + PointerDelta]) > 0) {
                                TempUcaseKey = store.UcaseKeyArray[FastPointer + PointerDelta];
                                tempPtrString = store.PointerArray[FastPointer + PointerDelta];
                                store.UcaseKeyArray[FastPointer + PointerDelta] = store.UcaseKeyArray[FastPointer];
                                store.PointerArray[FastPointer + PointerDelta] = store.PointerArray[FastPointer];
                                store.UcaseKeyArray[FastPointer] = TempUcaseKey;
                                store.PointerArray[FastPointer] = tempPtrString;
                                CleanPass = false;
                            }
                        }
                        if (CleanPass) {
                            break;
                        }
                    }
                }
                store.ArrayDirty = false;
            } catch (Exception ex) {
                throw new IndexException("BubbleSort error", ex);
            }
        }
        //
        //========================================================================
        //
        // Made by Michael Ciurescu (CVMichael from vbforums.com)
        // Original thread: http://www.vbforums.com/showthread.php?t=231925
        //
        //========================================================================
        //
        private void QuickSort()  {
            try {
                if (store.ArrayCount >= 2) {
                    QuickSort_Segment(store.UcaseKeyArray, store.PointerArray, 0, store.ArrayCount - 1);
                }
            } catch (Exception ex) {
                throw new IndexException("QuickSort error", ex);
            }
        }
        //
        //
        //========================================================================
        //
        // Made by Michael Ciurescu (CVMichael from vbforums.com)
        // Original thread: http://www.vbforums.com/showthread.php?t=231925
        //
        //========================================================================
        //
        private void QuickSort_Segment(string[] C, string[] P, int First, int Last) {
            try {
                int Low = 0;
                int High = 0;
                string MidValue = null;
                string TC = null;
                string TP = null;
                //
                Low = First;
                High = Last;
                MidValue = C[(First + Last) / 2];
                //
                do {
                    while (string.CompareOrdinal(C[Low], MidValue) < 0) {
                        Low = Low + 1;
                    }
                    while (string.CompareOrdinal(C[High], MidValue) > 0) {
                        High = High - 1;
                    }
                    if (Low <= High) {
                        TC = C[Low];
                        TP = P[Low];
                        C[Low] = C[High];
                        P[Low] = P[High];
                        C[High] = TC;
                        P[High] = TP;
                        Low = Low + 1;
                        High = High - 1;
                    }
                } while (Low <= High);
                if (First < High) {
                    QuickSort_Segment(C, P, First, High);
                }
                if (Low < Last) {
                    QuickSort_Segment(C, P, Low, Last);
                }
            } catch (Exception ex) {
                throw new IndexException("QuickSort_Segment error", ex);
            }
        }
        //
        //
        //
        private void Sort()  {
            try {
                QuickSort();
                store.ArrayDirty = false;
            } catch (Exception ex) {
                throw new IndexException("Sort error", ex);
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