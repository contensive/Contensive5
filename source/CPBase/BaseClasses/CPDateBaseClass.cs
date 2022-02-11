
using System;

namespace Contensive.BaseClasses {
    /// <summary>
    /// provate consistent JSON methods in a single updateable layer
    /// </summary>
    public abstract class CPDateBaseClass {
        //
        //==========================================================================================
        // As I said the other day, one important thing I've learned is to keep the webserver (and sql server) set to UTC (GMT). That means our applications need a setting for the time-zone for the "store"
        // Since all our components like ecommerce, blog, meetings, etc all have to store the timezone and work the same way, Im considering adding a cp.Date object with methods to get the store time and timezone, like
        // cp.Date.Now = the store's time
        // cp.Date.TimeZone = the store's timezone
        //
        // cp.Date.NowUtc
        //
        // cp.Date.Now( userId )
        // cp.Date.Now(PersonModel )
        // ... might return the user's local time.
        // I know these methods are pretty easy to generate manually, but having common cp.Date methods will reinforce that the dotnet datetime.now() will be the server's time, not Svetness's time, or the customers time.
        //
        // Would we then save dates in the db with cp.Date.NowUTC and have some sort of convert methods to display to the user, so like
        // cp.Date.ConvertToLocal(utcDateToConvert, userid)
        // cp.Date.ConvertToLocal(utcDateToConvert, userModel)
        // cp.Date.ConvertToLocal(utcDateToConvert, cp.Date.TimeZone)
        //
        // How would we collect user's timezones to put in their people records? Svetness just has a form that requires them to select one when they add a user.
        // Or would it be optional and cp.Date.Now(userModel) would return the date based on the user timezone, but if the user didn't have one, it would return based on the cp.Date.Timezone?
        //
        // I think that sounds good. The default timezone is the "store" timezone (there is a better word than "store", but I don't like "app")
        // We can add timezone to my-account, and we can probably get it from a javascript callback.I'm doing one right now to detect javascript support and cookie support. Maybe add it to that so we automatically know
        //==========================================================================================
        /// <summary>
        /// Not yet implemented. The TimeZone for the business's primary location (the store).
        /// <returns></returns>
        public abstract TimeZone TimeZone { get; }
        //
        //==========================================================================================
        /// <summary>
        /// Not yet implemented. The local date and time at the business's location (the store). If the business has multiple locations, this is the selected location or the primary.
        /// This is the server time in UTC plus the primary location time-zone.
        /// </summary>
        /// <returns></returns>
        public abstract DateTime Now { get; }
        //
        //==========================================================================================
        /// <summary>
        /// Not yet implemented. The current date and time in UTC (the server time). DateTime should be saved to the Db in UTC
        /// <returns></returns>
        public abstract DateTime NowUtc { get; }
        //
        //==========================================================================================
        /// <summary>
        /// Not yet implemented. The current date and time in UTC (the server time)
        /// <returns></returns>
        public abstract DateTime ConvertToLocal( DateTime utcDateToConvert, TimeZone localTimeZone  );
        //
        //==========================================================================================
        /// <summary>
        /// Not yet implemented. The current date and time for this user. 
        /// This is the server time in UTC plus the user's time-zone
        /// </summary>
        /// <returns></returns>
        public abstract DateTime UserNow(int userId);
    }
}

