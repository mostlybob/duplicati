﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4927
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Duplicati.GUI.Wizard_pages.Add_backup.Strings {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class CleanupSettings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CleanupSettings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Duplicati.GUI.Wizard_pages.Add_backup.Strings.CleanupSettings", typeof(CleanupSettings).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have disabled backup removal. If no backups are removed, the amount of stored data will increase forever.
        ///Disabling backup cleanup may result in storage space being exhausted.
        ///Do you want to continue without backup removal backups?.
        /// </summary>
        internal static string DisabledCleanupWarning {
            get {
                return ResourceManager.GetString("DisabledCleanupWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to To ensure that backups do not grow indefinately, Duplicati can automatically remove old backups..
        /// </summary>
        internal static string PageDescription {
            get {
                return ResourceManager.GetString("PageDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select cleanup options.
        /// </summary>
        internal static string PageTitle {
            get {
                return ResourceManager.GetString("PageTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The cleanup duration entered is less than 1 day. This will give very poor system performance..
        /// </summary>
        internal static string TooShortCleanupDurationDay {
            get {
                return ResourceManager.GetString("TooShortCleanupDurationDay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The full backup duration entered is less than ten minutes. This will give very poor system performance..
        /// </summary>
        internal static string TooShortFullDuration {
            get {
                return ResourceManager.GetString("TooShortFullDuration", resourceCulture);
            }
        }
    }
}