using System;

namespace Contensive.PageWidgetExample.Models.Domain {
    // 
    // ====================================================================================================
    /// <summary>
    /// Application environment class. Use to mock data and interfaces for unit tests. Pass this object into methods for dependency inject.
    /// For example .cache object requires construction, and siteProperties contain state that  obstructs unit testing.
    /// </summary>
    /// <remarks></remarks>
    public class ApplicationModel : IDisposable {
        // 
        // ====================================================================================================
        /// <summary>
        /// cp
        /// </summary>
        /// <returns></returns>
        public BaseClasses.CPBaseClass cp { get; set; }
        // 
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks></remarks>
        public ApplicationModel(BaseClasses.CPBaseClass cp, bool MockEmail = false) {
            this.cp = cp;
        }
        // 
        // ====================================================================================================
        // 
        public string imageNotAvailableAltSizeList {
            get {
                if (local_imageNotAvailableAltSizeList is null) {
                    local_imageNotAvailableAltSizeList = cp.CdnFiles.Read("PageWidgetExample/ImageNotAvailable-altSize.txt");
                }
                return local_imageNotAvailableAltSizeList;
            }
            set {
                cp.CdnFiles.Save("PageWidgetExample/ImageNotAvailable-altSize.txt", value);
            }
        }
        private string local_imageNotAvailableAltSizeList = null;

        // 
        // ====================================================================================================
        /// <summary>
        /// A cache asside instance for Items
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public Db.ItemModel itemAside(int itemId) {
            if (itemId <= 0)
                return null;
            if (_itemAside is null) {
                _itemAside = new CacheAsideModel();
            }
            return _itemAside.create<Db.ItemModel>(cp, itemId);
        }
        private CacheAsideModel _itemAside = null;
        // 
        // ====================================================================================================
        /// <summary>
        /// A cache asside instance for People
        /// </summary>
        /// <param name="personId"></param>
        /// <returns></returns>
        public Contensive.Models.Db.PersonModel peopleAside(int personId) {
            if (personId <= 0)
                return null;
            if (_peopleAside is null) {
                _peopleAside = new CacheAsideModel();
            }
            return _peopleAside.create<Contensive.Models.Db.PersonModel>(cp, personId);
        }
        private CacheAsideModel _peopleAside = null;
        // 
        // ====================================================================================================
        /// <summary>
        /// A cache asside instance for organizations -- NOT the order's organization
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        public Contensive.Models.Db.OrganizationModel organizationsAside(int organizationId) {
            if (organizationId <= 0)
                return null;
            if (_organizationsAside is null) {
                _organizationsAside = new CacheAsideModel();
            }
            return _organizationsAside.create<Contensive.Models.Db.OrganizationModel>(cp, organizationId);
        }
        private CacheAsideModel _organizationsAside = null;
        // 
        // 
        #region  IDisposable Support 
        protected bool disposed = false;
        // 
        // ==========================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks></remarks>
        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    // 
                    // ----- call .dispose for managed objects
                    // 
                    // If Not _cache Is Nothing Then
                    // _cache.Dispose()
                    // End If
                }
                // 
                // Add code here to release the unmanaged resource.
                // 
            }
            disposed = true;
        }
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~ApplicationModel() {
            Dispose(false);
        }
        #endregion
    }
}