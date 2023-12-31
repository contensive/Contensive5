
using System;
using System.Collections.Generic;
using Contensive.Models.Db;

namespace Contensive.CLI {
    //
    static class AddRootCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--addRoot [password]"
            + Environment.NewLine + "    Deletes the current user with username:root and creates a new developer user with un:root, password:set to match password requirements and returned, expiration:1 hr. Requires you first set the application with '-a appName'."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cpServer"></param>
        /// <param name="adminEmail"></param>
        /// <param name="adminPassword">the argument following the command</param>
        public static void execute(Processor.CPClass cpServer, string appName, string password) {
            if (string.IsNullOrEmpty(appName)) {
                //
                // -- invalid argument
                Console.Write(Environment.NewLine + "Invalid argument. --addRoot requires you first set the application with -a appname.");
                Console.Write(helpText);
                Console.Write(Environment.NewLine + "Run cc --help for a full list of commands.");
                return;
            }
            using (var cp = new Contensive.Processor.CPClass(appName)) {
                string username = "root";
                //
                // -- setup password
                if (string.IsNullOrEmpty(password)) { 
                    password = "C0ntensive!";
                    int minLength = cp.Site.GetInteger("password min length", 5);
                    if (password.Length < minLength) { password += new string('0', minLength - password.Length); }
                }
                //
                DbBaseModel.deleteRows<PersonModel>(cp, "(username=" + cp.Db.EncodeSQLText(username) + ")");
                var currentUser = DbBaseModel.addDefault<PersonModel>(cp);
                currentUser.name = "root";
                currentUser.firstName = "root";
                currentUser.email = "";
                currentUser.username = "root";
                currentUser.admin = true;
                currentUser.developer = true;
                currentUser.dateExpires = DateTime.Now.AddHours(1);
                currentUser.save(cp);
                string userError = "";
                if (!cp.User.SetPassword(password, currentUser.id, ref userError)) {
                    Console.Write(Environment.NewLine + "User reset but the password could not be saved. " + userError);
                } else {
                    Console.Write(Environment.NewLine + "User reset successfully.");
                    Console.Write(Environment.NewLine + "account login expires in 1 hour");
                    Console.Write(Environment.NewLine + "username: " + username);
                    Console.Write(Environment.NewLine + "password: " + password);
                }
                return;
            }
        }
    }
}
