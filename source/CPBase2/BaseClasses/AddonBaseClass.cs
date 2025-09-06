
namespace Contensive.BaseClasses {
    public abstract class AddonBaseClass
	{		
        /// <summary>
        /// The only exposed method of an addon. The method performs the required work and returns an object, typically a string. For addons executed in web page content, the returned string is added to the page where the addon is placed. When run as a remote method, the result is returned from the endpoint. For addons run as processes, the returned string is logged in the process log.
        /// </summary>
        /// <param name="CP">An instance of the CPBaseClass with a valid CP.MyAddon object pointing to the current addon parameters (values for this addon in the database)</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract object Execute(BaseClasses.CPBaseClass CP);
        ///// <summary>
        ///// consider 
        ///// </summary>
        //public string addonName { get; set; } = string.Empty;
    }
}

