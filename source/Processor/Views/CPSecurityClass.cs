
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor {
    public class CPSecurityClass : BaseClasses.CPSecurityBaseClass {
        //
        //
        private readonly Controllers.CoreController core;
        //
        // ====================================================================================================
        //
        public CPSecurityClass(Controllers.CoreController core) {
            this.core = core;
        }
        //
        // ====================================================================================================
        //
        public override string DecryptTwoWay(string encryptedString) {
            return SecurityController.decryptTwoWay(core, encryptedString);
        }
        //
        // ====================================================================================================
        //
        public override string EncryptOneWay(string unencryptedString) {
            return SecurityController.encryptOneWay(core, unencryptedString);
        }
        //
        // ====================================================================================================
        //
        public override string EncryptTwoWay(string unencryptedString) {
            return SecurityController.encryptTwoWay(core, unencryptedString);
        }
        //
        // ====================================================================================================
        //
        public override string GetRandomPassword() {
            throw new NotImplementedException();
        }
        //
        // ====================================================================================================
        //
        public override bool VerifyOneWay(string unencryptedString, string encryptedString) {
            return SecurityController.verifyOneWay(core, unencryptedString, encryptedString);
        }
    }
}