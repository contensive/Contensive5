
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Contensive.Addons.Tools.Constants;

namespace Contensive.Addons.Tools {
    namespace Views {
        //
        /// <summary>
        /// Class defines the addon. The Execute method is called.
        /// </summary>
        public class GetSampleDataList : AddonBaseClass {
            //
            /// <summary>
            /// addon execute method
            /// </summary>
            /// <param name="cp"></param>
            /// <returns></returns>
            public override object Execute(CPBaseClass cp) {
                try {
                    // 
                    // -- optional application object helper for aside cache, etc.
                    using Controllers.ApplicationController ae = new(cp);
                    // 
                    // -- get an object from the UI (javascript object stringified)
                    // -- first inject the fake data to simpulate UI input, then read it
                    SampleRequestObject objectValueFromUI = cp.JSON.Deserialize<SampleRequestObject>(cp.Doc.GetText("objectValueFromUI"));
                    // 
                    // -- create sample data
                    List<PersonModel> personList = DbBaseModel.createList<PersonModel>(cp);
                    // 
                    // -- add sample data to a node
                    ae.responseNodeList.Add(new Controllers.ResponseNodeClass() {
                        dataFor = "nameOfThisDataForUIToRecognize",
                        data = personList
                    });
                    return ae.getResponse();
                } catch (UnauthorizedAccessException) {
                    return Controllers.ApplicationController.getResponseUnauthorized(cp);
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex);
                    return Controllers.ApplicationController.getResponseServerError(cp);
                }
            }
            // 
            // =====================================================================================
            /// <summary>
            /// type matching structure of sample data read from the request body
            /// </summary>
            private class SampleRequestObject {
                /// <summary>
                /// return all people where this string matches their name
                /// </summary>
                public string searchText;
            }
        }
    }
}