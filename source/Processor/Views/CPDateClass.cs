
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
#pragma warning disable CS0618 // Suppress obsolete warning for required override
        public override TimeZone TimeZone => throw new NotImplementedException();
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // Suppress obsolete warning for required override
        public override DateTime ConvertToLocal(DateTime utcDateToConvert, TimeZone localTimeZone) {
            throw new NotImplementedException();
        }
#pragma warning restore CS0618
        //
        //====================================================================================================
        //
        public override DateTime UserNow(int userId) {
            throw new NotImplementedException();
        }
        //
        //====================================================================================================
        //
        public override TimeZoneInfo StoreTimeZone {
            get {
                // Return the store's configured time zone, or local system time zone as fallback
                return TimeZoneInfo.Local;
            }
        }
        //
        //====================================================================================================
        //
        public override DateTime ConvertToLocal(DateTime utcDateToConvert, TimeZoneInfo localTimeZone) {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateToConvert, localTimeZone);
        }
    }
}
