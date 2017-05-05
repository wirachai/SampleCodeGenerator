//------------------------------------------------------------------------------
// <copyright file="ToolWindowCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Contexts;
using CustomCodeGenerator.Helpers;
using CustomCodeGenerator.Templates;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CustomCodeGenerator.Commands.AddMapperClass
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddMapperCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("6b017566-555a-4430-96a9-da4182e0b2ab");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        public static DTE2 Dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMapperCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private AddMapperCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;
            Dte = ServiceProvider.GetService(typeof(DTE)) as DTE2;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(ShowToolWindow, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddMapperCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new AddMapperCommand(package);
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {
            //ToolWindowPane window = _package.FindToolWindow(typeof(AddMapperControl), 0, true);
            //if (window?.Frame == null)
            //{
            //    throw new NotSupportedException("Cannot create tool window");
            //}

            //IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            var activeProject = Dte.GetActiveProject();
            var model = new AddMapperViewModel(activeProject);

            AddMapperWindow window = new AddMapperWindow(model);
            bool? result = window.ShowDialog();
            if (result.HasValue && result == true)
            {
                // generate code here
                var rootNamespace = activeProject.GetRootNamespace();
                var folderName = "Mapper";
                var namespaceName = rootNamespace + "." + folderName;
                var className =
                    $"{model.SelectedSourceModelType.ShortTypeName}To{model.SelectedDestinationModelType.ShortTypeName}Mapper";
                var session = new Dictionary<string, object>()
                {
                    { "Namespace", namespaceName },
                    { "ClassName", className },
                    { "Source", model.SelectedSourceModelType.CodeType },
                    { "Destination", model.SelectedDestinationModelType.CodeType}
                };

                var template = new MapperClassTemplate();
                template.Session = session;
                template.Initialize();
                var generatedCode = template.TransformText();

                var rootFolder = activeProject.GetRootFolder(Dte);
                var outputFolder = Path.Combine(rootFolder, folderName);
                var outputFilePath = Path.Combine(outputFolder, className + ".cs");
                if (Directory.Exists(outputFolder) == false)
                {
                    Directory.CreateDirectory(outputFolder);
                    activeProject.AddDirectoryToProject(Dte, outputFolder);
                }
                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }
                File.WriteAllText(outputFilePath, generatedCode);
                activeProject.AddFileToProject(Dte, outputFilePath);
            }
        }
    }
}
