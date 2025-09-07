
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// for checklist display and processing
    /// </summary>
    public class RuleIdSecondaryIdModel {
        //
        // ====================================================================================================
        /// <summary>
        /// The hashed key
        /// </summary>
        public int  ruleId { get; set; }
        /// <summary>
        /// The original key before hashing
        /// </summary>
        public int secondaryId { get; set; }
    }
}