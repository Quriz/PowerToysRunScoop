﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Community.PowerToys.Run.Plugin.Scoop.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Community.PowerToys.Run.Plugin.Scoop.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Open homepage (Ctrl+H).
        /// </summary>
        public static string context_menu_homepage {
            get {
                return ResourceManager.GetString("context_menu_homepage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Uninstall (Ctrl+D).
        /// </summary>
        public static string context_menu_uninstall {
            get {
                return ResourceManager.GetString("context_menu_uninstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Update (Ctrl+U).
        /// </summary>
        public static string context_menu_update {
            get {
                return ResourceManager.GetString("context_menu_update", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Scoop package manager integration.
        /// </summary>
        public static string plugin_description {
            get {
                return ResourceManager.GetString("plugin_description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Search, install, update, and uninstall Scoop packages..
        /// </summary>
        public static string plugin_description_sub {
            get {
                return ResourceManager.GetString("plugin_description_sub", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to get API key for search..
        /// </summary>
        public static string plugin_error_api_key {
            get {
                return ResourceManager.GetString("plugin_error_api_key", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If you believe this is a plugin error, press Enter to open an issue..
        /// </summary>
        public static string plugin_error_api_key_sub {
            get {
                return ResourceManager.GetString("plugin_error_api_key_sub", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to open {0}..
        /// </summary>
        public static string plugin_error_homepage {
            get {
                return ResourceManager.GetString("plugin_error_homepage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Scoop.
        /// </summary>
        public static string plugin_name {
            get {
                return ResourceManager.GetString("plugin_name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No result found for &apos;{0}&apos;..
        /// </summary>
        public static string plugin_no_result {
            get {
                return ResourceManager.GetString("plugin_no_result", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maybe try something different..
        /// </summary>
        public static string plugin_no_result_sub {
            get {
                return ResourceManager.GetString("plugin_no_result_sub", resourceCulture);
            }
        }
    }
}
