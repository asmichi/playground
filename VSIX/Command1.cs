//------------------------------------------------------------------------------
// <copyright file="Command1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using Microsoft.VisualStudio;

namespace VSIXProject1
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command1
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("c36ae883-9fad-40c2-bb65-d9466effe5d4");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command1"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private Command1(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command1 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new Command1(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback0(object sender, EventArgs e)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "Command1";

            //EnvDTE.E
            //EnvDTE.Globals.
            var sp = this.ServiceProvider;
            var monitorSelection = sp.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = sp.GetService(typeof(SVsSolution)) as IVsSolution;
            var vcxprojGuid = new Guid("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}");
            IEnumHierarchies eh;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                solution.GetProjectEnum((uint)(__VSENUMPROJFLAGS.EPF_ALLINSOLUTION | __VSENUMPROJFLAGS.EPF_MATCHTYPE), vcxprojGuid, out eh));

            var hierarchies = new List<IVsHierarchy>();
            {
                var hierarchy = new IVsHierarchy[1];
                uint fetched;
                while (true)
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                        eh.Next(1, hierarchy, out fetched));
                    if (fetched <= 0)
                        break;
                    hierarchies.Add(hierarchy[0]);
                }
            }
            
            foreach (var h in hierarchies)
            {
                var p = GetDTEProject(h);
                var ppp = p.Properties;

                dynamic vcpd = p.Object;
                dynamic confs = (System.Collections.IEnumerable)vcpd.Configurations;
                foreach (dynamic conf in confs)
                {
                    dynamic props = (System.Collections.IEnumerable)conf.PropertySheets;
                    foreach (dynamic prop in props)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "{0}, {1}, {2}", (object)prop.Name, (object)prop.PropertySheetFile, (object)prop.IsSystemPropertySheet);
                    }
                }
                System.Diagnostics.Debug.WriteLine(p.FullName);
                //p.
            }


            //// Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.ServiceProvider,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            string solutionDir, solutionFile, optionFile;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                solution.GetSolutionInfo(out solutionDir, out solutionFile, out optionFile));
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                solution.CloseSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave, null, 0));
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                solution.OpenSolutionFile(0, solutionFile));
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Obtain the SVsSolution service object.
            var solutionService = this.ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            // Check if there exists an open solution.
            object isSolutionOpen;
            solutionService.GetProperty(
                (int)__VSPROPID.VSPROPID_IsSolutionOpen,
                out isSolutionOpen);
            if (!(bool)isSolutionOpen)
            {
                return;
            }

            // Retrieve the information on the open solution.
            string solutionDirectory;   // full path to the directory cotaining the .sln file
            string solutionFile;        // full path to the .sln file
            string userOptsFile;        // full path to the .suo file
            solutionService.GetSolutionInfo(
                out solutionDirectory,
                out solutionFile,
                out userOptsFile);

            // Close the entire solution. (pHier == null, docCookie == 0)
            solutionService.CloseSolutionElement(
                (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave, // prompts the user whether to save unsaved changes.
                null,
                0);

            // Reopen the solution.
            solutionService.OpenSolutionFile(0, solutionFile);
        }

        public static EnvDTE.Project GetDTEProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy");

            object obj;
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
            return obj as EnvDTE.Project;
        }
    }
}
