
using Contensive.BaseClasses;
using System;

namespace Contensive.Processor {
    public class CPDateClass : CPDateBaseClass {
        //
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPDateClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        //
        public override TimeZone TimeZone => throw new NotImplementedException();
        //
        //====================================================================================================
        //
        public override DateTime Now => throw new NotImplementedException();
        //
        //====================================================================================================
        //
        public override DateTime NowUtc => throw new NotImplementedException();
        //
        //====================================================================================================
        //
        public override DateTime ConvertToLocal(DateTime utcDateToConvert, TimeZone localTimeZone) {
            throw new NotImplementedException();
        }
        //
        //====================================================================================================
        //
        public override DateTime UserNow(int userId) {
            throw new NotImplementedException();
        }
    }
}
