
using System;

namespace Contensive.Models.Db {
    public class LoginByEmailOtpModel : DbBaseModel {
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Login By Email Otp", "LoginByEmailOtp", "default", false);
        public string email { get; set; }
        public string otp { get; set; }
        public DateTime expires { get; set; }
        public bool used { get; set; }
    }
}
