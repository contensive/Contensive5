
namespace Contensive.BaseClasses {
    public abstract class AddonBaseClass
	{
		/// <summary>
		/// The only exposed method of an addon. Performs the functions for this part of the the add-on and returns an object, typically a string. For add-ons executing on a web page or as a remove method, the returned string is added to the page where the addon is placed. For addons run as processes, the returned string is logged in the process log.
		/// </summary>
		/// <param name="CP">An instance of the CPBaseClass with a valid CP.MyAddon object pointing to the current addon parameters (values for this addon in the database)</param>
		/// <returns></returns>
		/// <remarks></remarks>
		public abstract object Execute(BaseClasses.CPBaseClass CP);
	}
}

