﻿
using Contensive.Processor;
//
namespace Contensive.Addons.Personalization {
    //
    public class NameClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return ((CPClass)cpBase).core.session.user.name;
        }
    }
}