
namespace Contensive.Processor.Addons.Login {
    /// <summary>
    /// Addon that returns a login button form, or if you are logged in, a link to my account and a logout button
    /// </summary>
    public class GetLoginButtonFormClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Addon that returns a login button form, or if you are logged in, a link to my account and a logout button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            if ( cp.User.IsAuthenticated ) {
                return Properties.Resources.LogoutButtonFormHtml.Replace("{{personName}}", cp.User.Name );
            }
            return Properties.Resources.LoginButtonFormHtml;
        }
    }
}
