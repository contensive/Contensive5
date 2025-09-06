
namespace Contensive.BaseClasses {
    public abstract class CPSecurityBaseClass {
        //
        //==========================================================================================
        //
        public abstract string GetRandomPassword();
        //
        //==========================================================================================
        /// <summary>
        /// return an encrypted string. This is a hash.
        /// One-way encryption cannot be reversed. 
        /// This encrypted string is repeatable, so run the same encryption twice and the encryption will match
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns></returns>
        public abstract string EncryptOneWay(string unencryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// return an encrypted string. This is a hash.
        /// One-way encryption cannot be reversed. 
        /// This encrypted string is repeatable, so run the same encryption twice and the encryption will match
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
        /// Return an AES encrypted string. This is a symetric encryption.
        /// A value encrypted, can be decrypted back to the same string
        /// The result is not repeatable. If you encrypt the same source twice, the result may not be the same each time.
        /// </summary>
        /// <param name="unencryptedString"></param>
        /// <returns></returns>
        public abstract string EncryptTwoWay(string unencryptedString);
        //
        //==========================================================================================
        /// <summary>
        /// Return an AES decrypted string. This is a symetric encryption.
        /// A value encrypted, can be decrypted back to the same string
        /// The result is not repeatable. If you encrypt the same source twice, the result may not be the same each time.
        /// </summary>
        /// <param name="encryptedString"></param>
        /// <returns></returns>
        public abstract string DecryptTwoWay(string encryptedString);
    }
}

