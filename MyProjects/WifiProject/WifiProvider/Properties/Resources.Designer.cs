﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WifiProvider.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WifiProvider.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to 1¶1¶%s1 already registered.
        ///1¶2¶User can not be found. Please check and try again.
        ///1¶3¶Hello, &lt;a href=&quot;./ChangePassword2?RPC=%s1&quot;&gt;click the link for change your password.&lt;/a&gt;Thank You.
        ///1¶4¶Your reset password mail send succesfully. Please click link on that mail.
        ///1¶5¶Error occured. Error record number : %s1
        ///1¶6¶Please check your internet connection.
        ///1¶7¶Signup completed succesfully. You can sign in now.§OK
        ///1¶8¶Please enter a valid e-mail address.
        ///1¶9¶Please enter your password.
        ///1¶10¶Application mus [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Dict {
            get {
                return ResourceManager.GetString("Dict", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to E-Posta : .
        /// </summary>
        internal static string LoginEmail {
            get {
                return ResourceManager.GetString("LoginEmail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parola : .
        /// </summary>
        internal static string LoginPass {
            get {
                return ResourceManager.GetString("LoginPass", resourceCulture);
            }
        }
    }
}
