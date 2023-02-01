
namespace Contensive.BaseClasses {
    public abstract class CPSecurityBaseClass {
        //
        //==========================================================================================
        //
        public abstract string GetRandomPassword();
        //
        //==========================================================================================
        /// <summary>
        /// return an encrypted string. One-way encryption cannot be reversed. This is a hash.
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns></returns>
        public abstract string EncryptOneWay(string unencryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// return an encrypted string. One-way encryption cannot be reversed. This is a hash.
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public abstract string EncryptOneWay(string unencryptedString, string salt);
        //
        //==========================================================================================
        //
        /// <summary>
        /// return true if an encrypted string matches an unencrypted string.
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        public abstract bool VerifyOneWay(string unencryptedString, string encryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// Return an AES encrypted string.  This is a two way so use it for little sister security, not foreign government security
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns></returns>
        public abstract string EncryptTwoWay(string unencryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// Decrypt an AES encrypted string. This is a two way so use it for little sister security, not foreign government security
        /// </summary>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        public abstract string DecryptTwoWay(string encryptedString);
    }
}

