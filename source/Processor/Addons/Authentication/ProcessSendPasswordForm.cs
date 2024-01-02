////
//using System;
//using Contensive.Processor.Controllers;
////
//namespace Contensive.Processor.Addons.Primitives {
//    public class ProcessPasswordRecoveryFormClass : Contensive.BaseClasses.AddonBaseClass {
//        //
//        //====================================================================================================
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="cp"></param>
//        /// <returns></returns>
//        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
//            string result = "";
//            try {
//                CoreController core = ((CPClass)cp).core;
//                //
//                PasswordRecoveryController.processPasswordRecoveryForm(core);
//            } catch (Exception ex) {
//                cp.Site.ErrorReport(ex);
//            }
//            return result;
//        }
//    }
//}
