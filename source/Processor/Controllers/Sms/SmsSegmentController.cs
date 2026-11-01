
using Contensive.BaseModels;
using System;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Calculate the number of SMS segments a message body uses. Pure static, no dependencies.
    /// GSM-7 basic set plus extension set (extension characters count 2). If every character is GSM: 1 segment up to 160 units, else ceil(units/153).
    /// Otherwise UCS-2: 1 segment up to 70 UTF-16 code units, else ceil(units/67).
    /// The aoCrm javascript counter mirrors this table. The core value is authoritative.
    /// </summary>
    public static class SmsSegmentController {
        //
        /// <summary>
        /// GSM 03.38 basic character set, written with unicode escapes so the source file encoding does not matter
        /// </summary>
        private const string gsmBasicCharacters = "@\u00A3$\u00A5\u00E8\u00E9\u00F9\u00EC\u00F2\u00C7\n\u00D8\u00F8\r\u00C5\u00E5\u0394_\u03A6\u0393\u039B\u03A9\u03A0\u03A8\u03A3\u0398\u039E\u00C6\u00E6\u00DF\u00C9 !\"#\u00A4%&'()*+,-./0123456789:;<=>?\u00A1ABCDEFGHIJKLMNOPQRSTUVWXYZ\u00C4\u00D6\u00D1\u00DC\u00A7\u00BFabcdefghijklmnopqrstuvwxyz\u00E4\u00F6\u00F1\u00FC\u00E0";
        /// <summary>
        /// GSM 03.38 extension characters, each counts as 2 units
        /// </summary>
        private const string gsmExtensionCharacters = "^{}\\[~]|\u20AC";
        //
        public const string encodingGsm7 = "GSM-7";
        public const string encodingUcs2 = "UCS-2";
        //
        //====================================================================================================
        /// <summary>
        /// Get the character units, segment count and encoding for a message body. A null or empty body returns 0 characters, 0 segments, GSM-7.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static SmsSegmentInfoModel getInfo(string body) {
            if (string.IsNullOrEmpty(body)) {
                return new SmsSegmentInfoModel { characters = 0, segments = 0, encoding = encodingGsm7 };
            }
            int gsmUnits = 0;
            bool isGsm = true;
            foreach (char c in body) {
                if (gsmBasicCharacters.IndexOf(c) >= 0) {
                    gsmUnits += 1;
                } else if (gsmExtensionCharacters.IndexOf(c) >= 0) {
                    gsmUnits += 2;
                } else {
                    isGsm = false;
                    break;
                }
            }
            if (isGsm) {
                return new SmsSegmentInfoModel {
                    characters = gsmUnits,
                    segments = gsmUnits <= 160 ? 1 : (int)Math.Ceiling(gsmUnits / 153.0),
                    encoding = encodingGsm7
                };
            }
            //
            // -- UCS-2, string.Length counts UTF-16 code units (an emoji is 2)
            int ucsUnits = body.Length;
            return new SmsSegmentInfoModel {
                characters = ucsUnits,
                segments = ucsUnits <= 70 ? 1 : (int)Math.Ceiling(ucsUnits / 67.0),
                encoding = encodingUcs2
            };
        }
    }
}
