﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GitTrends.Mobile.Common.Constants {
    using System;
    using System.Reflection;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class PullToRefreshFailedConstants {
        
        private static System.Resources.ResourceManager resourceMan;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal PullToRefreshFailedConstants() {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Resources.ResourceManager ResourceManager {
            get {
                if (object.Equals(null, resourceMan)) {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("GitTrends.Mobile.Common.Constants.PullToRefreshFailedConstants", typeof(PullToRefreshFailedConstants).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string UnableToConnectToGitHub {
            get {
                return ResourceManager.GetString("UnableToConnectToGitHub", resourceCulture);
            }
        }
        
        public static string UsageLimitExceeded {
            get {
                return ResourceManager.GetString("UsageLimitExceeded", resourceCulture);
            }
        }
        
        public static string GitHubApiLimit {
            get {
                return ResourceManager.GetString("GitHubApiLimit", resourceCulture);
            }
        }
        
        public static string MinutesReset {
            get {
                return ResourceManager.GetString("MinutesReset", resourceCulture);
            }
        }
        
        public static string LearnMore {
            get {
                return ResourceManager.GetString("LearnMore", resourceCulture);
            }
        }
        
        public static string AbuseLimitReached {
            get {
                return ResourceManager.GetString("AbuseLimitReached", resourceCulture);
            }
        }
        
        public static string GitHubApiAbuseLimit {
            get {
                return ResourceManager.GetString("GitHubApiAbuseLimit", resourceCulture);
            }
        }
        
        public static string AbuseLimitAutomaticRetry {
            get {
                return ResourceManager.GetString("AbuseLimitAutomaticRetry", resourceCulture);
            }
        }
    }
}
