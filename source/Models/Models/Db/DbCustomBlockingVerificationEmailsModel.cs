using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Models.Db
{
    public class DbCustomBlockingVerificationEmailsModel : DbBaseModel
    {
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Custom Blocking Verification Emails", "CustomBlockingVerificationEmails", "default", false);
        public string emailSentTo { get; set; }
    }
}
