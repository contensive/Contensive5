
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// doc properties are properties limited in scope to this single hit, or viewing
    /// </summary>
    public class DocPropertiesModel {
        //
        private readonly CoreController core;
        //
        private readonly Dictionary<string, DocPropertyModel> docPropertiesDict;
        //
        //====================================================================================================
        /// <summary>
        /// constuct
        /// </summary>
        /// <param name="core"></param>
        public DocPropertiesModel(CoreController core) {
            this.core = core;
            docPropertiesDict = new Dictionary<string, DocPropertyModel>();
        }
        //
        //====================================================================================================
        /// <summary>
        /// set integer property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        public void setProperty(string key, int value, DocPropertyModel.DocPropertyTypesEnum propertyType) {
            setProperty(key, value.ToString(), propertyType);
        }
        /// <summary>
        /// set integer property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setProperty(string key, int value) {
            setProperty(key, value.ToString(), DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set number (double) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        public void setProperty(string key, double value, DocPropertyModel.DocPropertyTypesEnum propertyType) {
            setProperty(key, value.ToString(), propertyType);
        }
        /// <summary>
        /// set number (double) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setProperty(string key, double value) {
            setProperty(key, value.ToString(), DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set date (DateTime) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        public void setProperty(string key, DateTime value, DocPropertyModel.DocPropertyTypesEnum propertyType) {
            setProperty(key, value.ToString(), propertyType);
        }
        /// <summary>
        /// set date (DateTime) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setProperty(string key, DateTime value) {
            setProperty(key, value.ToString(CultureInfo.InvariantCulture), DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set boolean (bool) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        public void setProperty(string key, bool value, DocPropertyModel.DocPropertyTypesEnum propertyType) {
            setProperty(key, value.ToString(), propertyType);
        }
        /// <summary>
        /// set boolean (bool) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setProperty(string key, bool value) {
            setProperty(key, value.ToString(), DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set text (string) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void setProperty(string key, string value) {
            setProperty(key, value, DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set text (string) property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="propertyType"></param>
        public void setProperty(string key, string value, DocPropertyModel.DocPropertyTypesEnum propertyType) {
            try {
                setProperty(key, new DocPropertyModel() {
                    nameValue = key + "=" + value,
                    fileSize = 0,
                    fileType = "",
                    name = key,
                    propertyType = propertyType,
                    value = value
                });
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Set complete doc property object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">See DocPropertyModel for details.</param>
        public void setProperty(string key, DocPropertyModel value) {
            //
            LogController.logTrace(core, "docPropertyModel.setProperty, nameValue [" + value.nameValue + "], propertyType [" + (int)value.propertyType + "]");
            //
            string propKey = encodeDocPropertyKey(key);
            if (!string.IsNullOrEmpty(propKey)) {
                if (docPropertiesDict.ContainsKey(propKey)) {
                    docPropertiesDict.Remove(propKey);
                }
                docPropertiesDict.Add(propKey, value);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the doc property key exists
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public bool containsKey(string Key) {
            return docPropertiesDict.ContainsKey(encodeDocPropertyKey(Key));
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get list of doc property keys
        /// </summary>
        /// <returns></returns>
        public List<string> getKeyList() {
            List<string> keyList = new List<string>();
            foreach (KeyValuePair<string, DocPropertyModel> kvp in docPropertiesDict) {
                keyList.Add(kvp.Key);
            }
            return keyList;
        }
        //
        //=============================================================================================
        /// <summary>
        /// get a doc property converted as Number (type double)
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public double getNumber(string RequestName) {
            try {
                return GenericController.encodeNumber(getProperty(RequestName).value);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================================
        /// <summary>
        /// get a doc property converted as integer
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public int getInteger(string RequestName) {
            try {
                return GenericController.encodeInteger(getProperty(RequestName).value);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================================
        /// <summary>
        /// get a doc property converted as Text (type string)
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public string getText(string RequestName) {
            try {
                return GenericController.encodeText(getProperty(RequestName).value);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================================
        /// <summary>
        /// get a doc property from they key, and decode the wysiwyg editor response into content to be saved.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string getTextFromWysiwygEditor(string key) {
            try {
                return ActiveContentController.processWysiwygResponseForSave(core, GenericController.encodeText(getProperty(key).value));
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================================
        /// <summary>
        /// get a doc property converted as Boolean (type bool)
        /// </summary>
        /// <param name="RequestName"></param>
        /// <returns></returns>
        public bool getBoolean(string RequestName) {
            try {
                return GenericController.encodeBoolean(getProperty(RequestName).value);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================================
        //
        public DateTime getDate(string RequestName) {
            try {
                return GenericController.encodeDate(getProperty(RequestName).value);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public DocPropertyModel getProperty(string RequestName) {
            try {
                string Key = encodeDocPropertyKey(RequestName);
                if (!string.IsNullOrEmpty(Key)) {
                    if (docPropertiesDict.ContainsKey(Key)) {
                        return docPropertiesDict[Key];
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return new DocPropertyModel();
        }
        //
        //====================================================================================================
        /// <summary>
        /// normalize/standardize a key for use in docproperties. Blank keys are skipped because they may be a data issue, not a code issue
        /// </summary>
        /// <param name="sourceKey"></param>
        /// <returns></returns>
        private string encodeDocPropertyKey(string sourceKey) {
            string returnResult = "";
            try {
                if (!string.IsNullOrEmpty(sourceKey)) {
                    returnResult = sourceKey.ToLowerInvariant();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        //
        //
        //
        //==========================================================================================
        /// <summary>
        /// add querystring to the doc properties
        /// </summary>
        /// <param name="QS"></param>
        public void addQueryString(string QS) {
            try {
                string[] ampSplit = QS.Split('&');
                for (int Ptr = 0; Ptr < ampSplit.GetUpperBound(0) + 1; Ptr++) {
                    string nameValuePair = ampSplit[Ptr];
                    if (!string.IsNullOrEmpty(nameValuePair)) {
                        if (GenericController.strInstr(1, nameValuePair, "=") != 0) {
                            string[] ValuePair = nameValuePair.Split('=');
                            string key = decodeResponseVariable(encodeText(ValuePair[0]));
                            if (!string.IsNullOrEmpty(key)) {
                                DocPropertyModel docProperty = new DocPropertyModel {
                                    name = key,
                                    propertyType = DocPropertyModel.DocPropertyTypesEnum.queryString,
                                    value = (ValuePair.GetUpperBound(0) > 0) ? decodeResponseVariable(encodeText(ValuePair[1])) : ""
                                };
                                core.docProperties.setProperty(key, docProperty);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the docProperties collection as the legacy optionString
        /// </summary>
        /// <returns></returns>
        public string getLegacyOptionStringFromVar() {
            var returnString = new StringBuilder();
            foreach (string key in getKeyList()) {
                returnString.Append("&" + GenericController.encodeLegacyOptionStringArgument(key) + "=" + encodeLegacyOptionStringArgument(getProperty(key).value));
            }
            return returnString.ToString();
        }
    }
}