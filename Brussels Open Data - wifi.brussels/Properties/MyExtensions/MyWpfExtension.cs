namespace OpenData
{
    //#if _MyType != "Empty"
#if _MyType != _MyTypeSomeValue

    namespace My
    {
        /// <summary>
        /// Module utilisé pour définir les propriétés qui sont disponibles dans l'espace de noms My pour WPF
        /// </summary>
        /// <remarks></remarks>

        internal static class MyWpfExtension
        {
            private static ThreadSafeObjectProvider<Microsoft.VisualBasic.Devices.Computer> s_Computer = new ThreadSafeObjectProvider<Microsoft.VisualBasic.Devices.Computer>();
            private static ThreadSafeObjectProvider<Microsoft.VisualBasic.ApplicationServices.User> s_User = new ThreadSafeObjectProvider<Microsoft.VisualBasic.ApplicationServices.User>();
            private static ThreadSafeObjectProvider<MyWindows> s_Windows = new ThreadSafeObjectProvider<MyWindows>();
            private static ThreadSafeObjectProvider<Microsoft.VisualBasic.Logging.Log> s_Log = new ThreadSafeObjectProvider<Microsoft.VisualBasic.Logging.Log>();

            /// <summary>
            /// Retourne l'objet application pour l'application en cours d'exécution
            /// </summary>
            [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Application Application
            {
                get
                {
                    return (Application)global::System.Windows.Application.Current;
                }
            }

            /// <summary>
            /// Retourne les informations relatives à l'ordinateur hôte.
            /// </summary>
            [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Microsoft.VisualBasic.Devices.Computer Computer
            {
                get
                {
                    return s_Computer.GetInstance();
                }
            }

            /// <summary>
            /// Retourne les informations relatives à l'utilisateur actuel.  Si vous souhaitez exécuter l'application avec les
            /// informations d'identification de l'utilisateur Windows, appelez My.User.InitializeWithWindowsUser().
            /// </summary>
            [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Microsoft.VisualBasic.ApplicationServices.User User
            {
                get
                {
                    return s_User.GetInstance();
                }
            }

            /// <summary>
            /// Retourne le journal des applications. L'écouteur peut être configuré par le fichier de configuration de l'application.
            /// </summary>
            [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static Microsoft.VisualBasic.Logging.Log Log
            {
                get
                {
                    return s_Log.GetInstance();
                }
            }

            /// <summary>
            /// Retourne la collection de fenêtres définie dans le projet.
            /// </summary>
            [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal static MyWindows Windows
            {
                [global::System.Diagnostics.DebuggerHidden()]
                get
                {
                    return s_Windows.GetInstance();
                }
            }

            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never), Microsoft.VisualBasic.MyGroupCollection("System.Windows.Window", "Create__Instance__", "Dispose__Instance__", "My.MyWpfExtenstionModule.Windows")]
            internal sealed class MyWindows
            {
                [global::System.Diagnostics.DebuggerHidden()]
                private static T Create__Instance__<T>(T Instance) where T : global::System.Windows.Window, new()
                {
                    if (Instance == null)
                    {
                        if (s_WindowBeingCreated != null)
                        {
                            if (s_WindowBeingCreated.ContainsKey(typeof(T)) == true)
                            {
                                throw new global::System.InvalidOperationException("The window cannot be accessed via My.Windows from the Window constructor.");
                            }
                        }
                        else
                        {
                            s_WindowBeingCreated = new global::System.Collections.Hashtable();
                        }
                        s_WindowBeingCreated.Add(typeof(T), null);
                        return new T();
                        //s_WindowBeingCreated.Remove(typeof(T));
                    }
                    else
                    {
                        return Instance;
                    }
                }

                [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), global::System.Diagnostics.DebuggerHidden()]
                private void Dispose__Instance__<T>(ref T instance) where T : global::System.Windows.Window
                {
                    instance = null;
                }

                [global::System.Diagnostics.DebuggerHidden(), global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                public MyWindows() : base()
                {
                }

                [global::System.ThreadStatic()]
                private static global::System.Collections.Hashtable s_WindowBeingCreated;

                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                public override bool Equals(object o)
                {
                    return base.Equals(o);
                }

                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }

                [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal new global::System.Type GetType()
                {
                    return typeof(MyWindows);
                }

                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                public override string ToString()
                {
                    return base.ToString();
                }
            }
        }
    }

    public partial class Application : global::System.Windows.Application
    {
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        internal Microsoft.VisualBasic.ApplicationServices.AssemblyInfo Info
        {
            [global::System.Diagnostics.DebuggerHidden()]
            get
            {
                return new Microsoft.VisualBasic.ApplicationServices.AssemblyInfo(global::System.Reflection.Assembly.GetExecutingAssembly());
            }
        }
    }

#endif
}