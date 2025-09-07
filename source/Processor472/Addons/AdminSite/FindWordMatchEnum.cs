
namespace Contensive.Processor.Addons.AdminSite {
    //
    //====================================================================================================
    /// <summary>
    /// admin filter methods (includes, equals, greaterthan, etc)
    /// </summary>
    public enum FindWordMatchEnum {
        /// <summary>
        /// ignore filter
        /// </summary>
        MatchIgnore = 0,
        /// <summary>
        /// must be empty
        /// </summary>
        MatchEmpty = 1,
        /// <summary>
        /// find not-empty
        /// </summary>
        MatchNotEmpty = 2,
        /// <summary>
        /// find greater than
        /// </summary>
        MatchGreaterThan = 3,
        /// <summary>
        /// find less than
        /// </summary>
        MatchLessThan = 4,
        /// <summary>
        /// includes this word
        /// </summary>
        matchincludes = 5,
        /// <summary>
        /// exactly equals
        /// </summary>
        MatchEquals = 6,
        /// <summary>
        /// find true values
        /// </summary>
        MatchTrue = 7,
        /// <summary>
        /// find false values
        /// </summary>
        MatchFalse = 8
    }
}
