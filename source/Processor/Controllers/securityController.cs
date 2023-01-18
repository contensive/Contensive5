
using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Processor.Exceptions;
using System.Linq;
//
namespace Contensive.Processor.Controllers {
    public class SecurityController {
        //
        /// <summary>
        /// Two way encrypt ciphers. des is older and aes should be used. Both are provided for possible migration.
        /// </summary>
        public enum TwoWayCiphers {
            des = 0,
            aes = 1
        }
        //
        //========================================================================
        /// <summary>
        /// create an encrypted token with an integer and a date
        /// </summary>
        /// <param name="core"></param>
        /// <param name="keyInteger"></param>
        /// <param name="expiresDate"></param>
        /// <returns></returns>
        public static string encodeToken(CoreController core, int keyInteger, DateTime expiresDate) {
            try {
                return encryptTwoWay(core, keyInteger.ToString() + "\t" + expiresDate.ToString("yyyy-MM-dd'T'HH:mm:ss"));
            } catch (Exception ex) {
                LogController.logError(core, ex, "EncodeToken failure. Returning blank result for keyInteger [" + keyInteger + "], keyDate [" + expiresDate + "]");
                return "";
            }
        }
        //
        //========================================================================
        /// <summary>
        /// decode an encrypted token with an integer and a date.
        /// result is 0 if there was a decode error
        /// </summary>
        /// <param name="core"></param>
        /// <param name="token"></param>
        public static TokenData decodeToken(CoreController core, string token) {
            var result = new TokenData();
            try {
                string decodedString = decryptTwoWay(core, token);
                string[] parts = decodedString.Split(Convert.ToChar("\t"));
                if (parts.Length == 2) {
                    result.id = GenericController.encodeInteger(parts[0]);
                    result.expires = GenericController.encodeDate(parts[1]);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex, "DecodeToken failure. Returning blank result for token [" + token + "]");
            }
            return result;
        }
        /// <summary>
        /// token, initialize to 0 and mindate
        /// </summary>
        public class TokenData {
            public int id { get; set; }
            public DateTime expires { get; set; }
            public TokenData()  {
                id = 0;
                expires = DateTime.MinValue;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return an encrypted string. This is a one way so use it passwords, etc.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string encryptOneWay(CoreController core, string password, string salt) {
            string returnResult = "";
            try {
                byte[] emptySalt = Encoding.ASCII.GetBytes(salt);
                returnResult = HashEncode.computeHash(password, "SHA512", emptySalt);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return true if an encrypted string matches an unencrypted string.
        /// </summary>
        /// <param name="sourceToTest"></param>
        /// <returns></returns>
        public static bool verifyOneWay(CoreController core, string sourceToTest, string encryptedToken) {
            bool returnResult = false;
            try {
                returnResult = HashEncode.verifyHash(sourceToTest, "SHA512", encryptedToken);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return an encrypted string. This is a two way so use it for little sister security, not foreign government security
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceToEncrypt"></param>
        /// <param name="cipher">Select the cipher. Des is older, Aes is newer and more secure. The selection is provided </param>
        /// <returns></returns>
        public static string encryptTwoWay(CoreController core, string sourceToEncrypt, TwoWayCiphers cipher) {
            try {
                if (cipher == TwoWayCiphers.aes) {
                    return Crypto.encryptStringAES(sourceToEncrypt, core.appConfig.privateKey);
                } else {
                    return encryptDes(core, sourceToEncrypt);
                }
            } catch (Exception) {
                //
                // -- crypto errors should just return a blank response
                return string.Empty;
            }
        }
        /// <summary>
        /// Return an AES encrypted string. This is a two way so use it for little sister security, not foreign government security
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceToEncrypt"></param>
        /// <returns></returns>
        public static string encryptTwoWay(CoreController core, string sourceToEncrypt) => encryptTwoWay(core, sourceToEncrypt, TwoWayCiphers.aes);
        //
        //====================================================================================================
        /// <summary>
        /// Decrypt a token encrypted with encryptTwoWay
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceToDecrypt"></param>
        /// <param name="cipher"></param>
        /// <returns></returns>
        public static string decryptTwoWay(CoreController core, string sourceToDecrypt, TwoWayCiphers cipher) {
            try {
                if (cipher == TwoWayCiphers.aes) {
                    return Crypto.decryptStringAES(sourceToDecrypt, core.appConfig.privateKey);
                } else {
                    return decryptDes(core, sourceToDecrypt);
                }
            } catch (Exception) {
                //
                // -- crypto errors should just return a blank response
                return string.Empty;
            }
        }
        /// <summary>
        /// Decrypt a token encrypted with encryptTwoWay using AES
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sourceToDecrypt"></param>
        /// <returns></returns>
        public static string decryptTwoWay(CoreController core, string sourceToDecrypt) => decryptTwoWay(core, sourceToDecrypt, TwoWayCiphers.aes);
        //
        //====================================================================================================
        /// <summary>
        /// return a DES encrypted string. This is a two way so use it for little sister security, not foreign government security
        /// </summary>
        /// <param name="sourceToEncrypt"></param>
        /// <returns></returns>
        private static string encryptDes(CoreController core, string sourceToEncrypt) {
            string returnResult = "";
            try {
                if (string.IsNullOrEmpty(core.appConfig.privateKey)) {
                    //
                } else {
                    // Compute has key using DES
                    byte[] saltBytes = ASCIIEncoding.ASCII.GetBytes("notsorandomsalt");
                    byte[] key = ASCIIEncoding.ASCII.GetBytes(HashEncode.computeHash(core.appConfig.privateKey, "SHA512", saltBytes));
                    Array.Resize(ref key, 24);
                    TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider {
                        Key = key,
                        Mode = CipherMode.ECB
                    };
                    ICryptoTransform DESEncrypt = DES.CreateEncryptor();
                    byte[] Buffer = ASCIIEncoding.ASCII.GetBytes(sourceToEncrypt);
                    Buffer = DESEncrypt.TransformFinalBlock(Buffer, 0, Buffer.Length);
                    returnResult = Convert.ToBase64String(Buffer);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a DES decrypted string. blank or non-base64 strings return an empty string. Exception thrown if decryption error. This is a two way so use it for little sister security, not foreign government security
        /// </summary>
        /// <param name="sourceToDecrypt"></param>
        /// <returns></returns>
        private static string decryptDes(CoreController core, string sourceToDecrypt) {
            string returnResult = "";
            try {
                if (string.IsNullOrEmpty(sourceToDecrypt)) {
                    //
                    // -- source blank, decrypt to blank
                } else if (!sourceToDecrypt.isBase64String()) {
                    //
                    // -- source invalid, decrypt to blank
                } else {
                    byte[] saltBytes = ASCIIEncoding.ASCII.GetBytes("notsorandomsalt");
                    byte[] key = ASCIIEncoding.ASCII.GetBytes(HashEncode.computeHash(core.appConfig.privateKey, "SHA512", saltBytes));
                    Array.Resize(ref key, 24);
                    byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(core.appConfig.privateKey);
                    TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider {
                        Key = key,
                        Mode = CipherMode.ECB
                    };
                    ICryptoTransform DESDecrypt = DES.CreateDecryptor();
                    buffer = Convert.FromBase64String(sourceToDecrypt);
                    try {
                        returnResult = System.Text.ASCIIEncoding.ASCII.GetString(DESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
                    } catch (Exception ex) {
                        LogController.logError(core, ex);
                        throw;
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnResult;
        }
        /// <summary>
        /// A hash is a one-way conversion. This class is used throughout.
        /// This class generates and compares hashes using MD5, SHA1, SHA256, SHA384, 
        /// and SHA512 hashing algorithms. Before computing a hash, it appends a
        /// randomly generated salt to the plain text, and stores this salt appended
        /// to the result. To verify another plain text value against the given hash,
        /// this class will retrieve the salt value from the hash string and use it
        /// when computing a new hash of the plain text. Appending a salt value to
        /// the hash may not be the most efficient approach, so when using hashes in
        /// a real-life application, you may choose to store them separately. You may
        /// also opt to keep results as byte arrays instead of converting them into
        /// base64-encoded strings.
        /// </summary>
        private static class HashEncode {
            //
            //==========================================================================================
            /// <summary>
            /// Generates a hash for the given plain text value and returns a
            /// base64-encoded result. Before the hash is computed, a random salt
            /// is generated and appended to the plain text. This salt is stored at
            /// the end of the hash value, so it can be used later for hash
            /// verification.
            /// </summary>
            /// <param name="plainText">
            /// Plaintext value to be hashed. The function does not check whether
            /// this parameter is null.
            /// </param>
            /// <param name="hashAlgorithm">
            /// Name of the hash algorithm. Allowed values are: "MD5", "SHA1",
            /// "SHA256", "SHA384", and "SHA512" (if any other value is specified
            /// MD5 hashing algorithm will be used). This value is case-insensitive.
            /// </param>
            /// <param name="saltBytes">
            /// Salt bytes. Can be empty. If provided, the salt must be saved
            /// </param>
            /// <returns>
            /// Hash value formatted as a base64-encoded string.
            /// </returns>
            public static string computeHash(string plainText, string hashAlgorithm, byte[] saltBytes) {
                if (string.IsNullOrEmpty(plainText)) {
                    return "";
                }
                //
                // Convert plain text into a byte array.
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                //
                // Allocate array, which will hold plain text and salt.
                byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];
                //
                // Copy plain text bytes into resulting array.
                for (int i = 0; i < plainTextBytes.Length; i++) {
                    plainTextWithSaltBytes[i] = plainTextBytes[i];
                }
                //
                // Append salt bytes to the resulting array.
                for (int i = 0; i < saltBytes.Length; i++) {
                    plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
                }
                //
                // Because we support multiple hashing algorithms, we must define
                // hash object as a common (abstract) base class. We will specify the
                // actual hashing algorithm class later during object creation.
                HashAlgorithm hash = null;
                switch ((hashAlgorithm != null) ? hashAlgorithm.ToUpperInvariant() : "") {
                    case "SHA1": {
                            hash = new SHA1Managed();
                            break;
                        }
                    case "SHA256": {
                            hash = new SHA256Managed();
                            break;
                        }
                    case "SHA384": {
                            hash = new SHA384Managed();
                            break;
                        }
                    case "SHA512": {
                            hash = new SHA512Managed();
                            break;
                        }
                    default: {
                            hash = new MD5CryptoServiceProvider();
                            break;
                        }
                }
                //
                // Compute hash value of our plain text with appended salt.
                byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);
                //
                // Create array which will hold hash and original salt bytes.
                byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];
                //
                // Copy hash bytes into resulting array.
                for (int i = 0; i < hashBytes.Length; i++) {
                    hashWithSaltBytes[i] = hashBytes[i];
                }
                //
                // Append salt bytes to the result.
                for (int i = 0; i < saltBytes.Length; i++) {
                    hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];
                }
                //
                // Convert result into a base64-encoded string.
                string hashValue = Convert.ToBase64String(hashWithSaltBytes);
                //
                // Return the result.
                return hashValue;
            }

            //
            //====================================================================================================
            /// <summary>
            /// Compares a hash of the specified plain text value to a given hash
            /// value. Plain text is hashed with the same salt value as the original
            /// hash.
            /// </summary>
            /// <param name="plainText">
            /// Plain text to be verified against the specified hash. The function
            /// does not check whether this parameter is null.
            /// </param>
            /// <param name="hashAlgorithm">
            /// Name of the hash algorithm. Allowed values are: "MD5", "SHA1",
            /// "SHA256", "SHA384", and "SHA512" (if any other value is specified
            /// MD5 hashing algorithm will be used). This value is case-insensitive.
            /// </param>
            /// <param name="hashValue">
            /// Base64-encoded hash value produced by ComputeHash function. This value
            /// includes the original salt appended to it.
            /// </param>
            /// <returns>
            /// If computed hash mathes the specified hash the function the return
            /// value is true; otherwise, the function returns false.
            /// </returns>
            public static bool verifyHash(string plainText, string hashAlgorithm, string hashValue) {
                // Convert base64-encoded hash value into a byte array.
                byte[] hashWithSaltBytes = Convert.FromBase64String(hashValue);
                //
                // We must know size of hash (without salt).
                int hashSizeInBits = 0;
                switch ((hashAlgorithm != null) ? hashAlgorithm.ToUpperInvariant() : "") {
                    case "SHA1": {
                            hashSizeInBits = 160;
                            break;
                        }
                    case "SHA256": {
                            hashSizeInBits = 256;
                            break;
                        }
                    case "SHA384": {
                            hashSizeInBits = 384;
                            break;
                        }
                    case "SHA512": {
                            hashSizeInBits = 512;
                            break;
                        }
                    default: {// Must be MD5
                            hashSizeInBits = 128;
                            break;
                        }
                }
                // Convert size of hash from bits to bytes.
                int hashSizeInBytes = encodeInteger(hashSizeInBits / 8.0);
                //
                // Copy salt from the end of the hash to the new array.
                byte[] saltBytes = new byte[hashWithSaltBytes.Length - hashSizeInBytes];
                for (int i = 0; i < saltBytes.Length; i++) {
                    saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];
                }
                // Compute a new hash string.
                string expectedHashString = computeHash(plainText, hashAlgorithm, saltBytes);
                return (hashValue == expectedHashString);
            }
        }
        //
        //========================================================================
        // -- from https://stackoverflow.com/a/10366194/157247
        /// <summary>
        /// A class for creating a symetric encryption
        /// </summary>
        protected class Crypto {
            protected Crypto()  { }
            //
            // While an app specific salt is not the best practice for
            // password based encryption, it's probably safe enough as long as
            // it is truly uncommon. Also too much work to alter this answer otherwise.
            //
            private static readonly byte[] _salt = Encoding.ASCII.GetBytes("notreallyrandomsalt");
            //
            /// <summary>
            /// Encrypt the given string using AES.  The string can be decrypted using 
            /// DecryptStringAES().  The sharedSecret parameters must match.
            /// </summary>
            /// <param name="plainText">The text to encrypt.</param>
            /// <param name="sharedSecret">A password used to generate a key for encryption.</param>
            public static string encryptStringAES(string plainText, string sharedSecret) {
                string outStr = null;
                if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(sharedSecret)) {
                    outStr = "";
                } else {
                    RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.
                    try {
                        // generate the key from the shared secret and the salt
                        Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                        // Create a RijndaelManaged object
                        aesAlg = new RijndaelManaged();
                        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                        // Create a decryptor to perform the stream transform.
                        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                        // Create the streams used for encryption.
                        using (MemoryStream msEncrypt = new MemoryStream()) {
                            // prepend the IV
                            msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                            msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
                                    swEncrypt.Write(plainText);
                                }
                            }
                            outStr = Convert.ToBase64String(msEncrypt.ToArray());
                        }
                    } finally {
                        // Clear the RijndaelManaged object.
                        if (aesAlg != null)
                            aesAlg.Clear();
                    }
                }
                // Return the encrypted bytes from the memory stream.
                return outStr;
            }
            //
            /// <summary>
            /// Decrypt the given string.  Assumes the string was encrypted using 
            /// EncryptStringAES(), using an identical sharedSecret.
            /// </summary>
            /// <param name="cipherText">The text to decrypt.</param>
            /// <param name="sharedSecret">A password used to generate a key for decryption.</param>
            public static string decryptStringAES(string cipherText, string sharedSecret) {
                if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(sharedSecret) || !cipherText.isBase64String()) {
                    return "";
                } else {
                    // Declare the RijndaelManaged object
                    // used to decrypt the data.
                    RijndaelManaged aesAlg = null;

                    // Declare the string used to hold
                    // the decrypted text.
                    string plaintext = null;

                    try {
                        // generate the key from the shared secret and the salt
                        Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                        // Create the streams used for decryption.                
                        byte[] bytes = Convert.FromBase64String(cipherText);
                        using (MemoryStream msDecrypt = new MemoryStream(bytes)) {
                            // Create a RijndaelManaged object
                            // with the specified key and IV.
                            aesAlg = new RijndaelManaged();
                            aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                            // Get the initialization vector from the encrypted stream
                            aesAlg.IV = readByteArray(msDecrypt);
                            // Create a decrytor to perform the stream transform.
                            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                                using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                                    // Read the decrypted bytes from the decrypting stream
                                    // and place them in a string.
                                    plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    } finally {
                        // Clear the RijndaelManaged object.
                        if (aesAlg != null) {
                            aesAlg.Clear();
                        }
                    }
                    return plaintext;
                }
            }
            //
            private static byte[] readByteArray(Stream s) {
                byte[] rawLength = new byte[sizeof(int)];
                if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length) {
                    throw new GenericException("Stream did not contain properly formatted byte array");
                }
                byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                if (s.Read(buffer, 0, buffer.Length) != buffer.Length) {
                    throw new GenericException("Did not read byte array properly");
                }
                return buffer;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
