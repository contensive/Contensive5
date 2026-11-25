using System;
using System.Security.Principal;

namespace Contensive.CLI.Controllers {
    static class GenericController {
        //
        public static string promptForReply(string prompt, string currentValue, string defaultValue = "") {
            Console.Write(prompt + ": ");
            if (string.IsNullOrEmpty(currentValue)) currentValue = defaultValue;
            if (!string.IsNullOrEmpty(currentValue)) Console.Write("(" + currentValue + ")");
            string reply = Console.ReadLine();
            if (string.IsNullOrEmpty(reply)) reply = currentValue;
            return reply;
        }
        //
        /// <summary>
        /// Return true if the current process is running elevated (admin or SYSTEM).
        /// BuiltinAdministratorsSid covers a normal "Run as administrator" session.
        /// LocalSystemSid covers NT AUTHORITY\SYSTEM (Task Scheduler ServiceAccount).
        /// </summary>
        public static bool isElevated() {
            var owner = WindowsIdentity.GetCurrent().Owner;
            return owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)
                || owner.IsWellKnown(WellKnownSidType.LocalSystemSid);
        }
    }
}
