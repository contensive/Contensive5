using System;

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
    }
}
