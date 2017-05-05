using System;
using EnvDTE;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE80;

namespace CustomCodeGenerator.Helpers
{
    public static class ProjectHelper
    {
        public static List<string> GetNameSpaces(this Project project)
        {
            var namespaces = new List<string>();
            foreach (EnvDTE.ProjectItem item in GetProjectItemsRecursively(project.ProjectItems))
            {
                if (item.FileCodeModel == null) continue;
                foreach (EnvDTE.CodeElement elem in item.FileCodeModel.CodeElements)
                {
                    if (elem.Kind == EnvDTE.vsCMElement.vsCMElementNamespace
                        && !namespaces.Contains(elem.FullName))
                        namespaces.Add(elem.FullName);
                }
            }
            return namespaces;
        }

        public static IEnumerable<CodeNamespace> GetCodeNamespaces(this Project project, List<string> namespaces)
        {
            return project
                .CodeModel
                .CodeElements
                .OfType<EnvDTE.CodeNamespace>()
                .Where(cn => namespaces.Contains(cn.FullName));
        }

        private static List<CodeType> GetCodeTypes(IEnumerable<CodeNamespace> codeNamespaces)
        {
            var codeTypes = new List<CodeType>();
            foreach (EnvDTE.CodeNamespace ns in codeNamespaces)
            {
                foreach (EnvDTE.CodeType ct in ns.Members.OfType<EnvDTE.CodeType>())
                {
                    codeTypes.Add(ct);
                }
            }
            return codeTypes;
        }

        public static List<CodeType> GetCodeTypes(this Project project)
        {
            var namespaces = GetNameSpaces(project);
            var codeNamespaces = GetCodeNamespaces(project, namespaces);
            return GetCodeTypes(codeNamespaces);
        }

        private static List<EnvDTE.ProjectItem> GetProjectItemsRecursively(EnvDTE.ProjectItems items)
        {
            var ret = new List<EnvDTE.ProjectItem>();
            if (items == null) return ret;
            foreach (EnvDTE.ProjectItem item in items)
            {
                ret.Add(item);
                ret.AddRange(GetProjectItemsRecursively(item.ProjectItems));
            }
            return ret;
        }

        public static IEnumerable<Project> GetChildProjects(this Project parent)
        {
            try
            {
                if (!parent.IsKind(ProjectKinds.vsProjectKindSolutionFolder) && parent.Collection == null)  // Unloaded
                    return Enumerable.Empty<Project>();

                if (!string.IsNullOrEmpty(parent.FullName))
                    return new[] { parent };
            }
            catch (COMException)
            {
                return Enumerable.Empty<Project>();
            }

            return parent.ProjectItems
                .Cast<ProjectItem>()
                .Where(p => p.SubProject != null)
                .SelectMany(p => GetChildProjects(p.SubProject));
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (var guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string GetRootFolder(this Project project, DTE2 dte)
        {
            if (project == null)
                return null;

            if (project.IsKind(ProjectKinds.vsProjectKindSolutionFolder))
                return Path.GetDirectoryName(dte.Solution.FullName);

            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }

        public static ProjectItem AddFileToProject(this Project project, DTE2 dte, string fullName, string itemType = null)
        {
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT))
                return dte.Solution.FindProjectItem(fullName);

            var root = project.GetRootFolder(dte);

            if (string.IsNullOrEmpty(root) || !fullName.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;

            ProjectItem item = project.ProjectItems.AddFromFile(fullName);
            item.SetItemType(itemType);
            return item;
        }

        public static ProjectItem AddDirectoryToProject(this Project project, DTE2 dte, string folderName)
        {
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT))
                return dte.Solution.FindProjectItem(folderName);

            var root = project.GetRootFolder(dte);

            if (string.IsNullOrEmpty(root) || !folderName.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;

            ProjectItem item = project.ProjectItems.AddFromDirectory(folderName);
            return item;
        }

        public static void SetItemType(this ProjectItem item, string itemType)
        {
            try
            {
                if (item == null || item.ContainingProject == null)
                    return;

                if (string.IsNullOrEmpty(itemType)
                    || item.ContainingProject.IsKind(ProjectTypes.WEBSITE_PROJECT)
                    || item.ContainingProject.IsKind(ProjectTypes.UNIVERSAL_APP))
                    return;

                item.Properties.Item("ItemType").Value = itemType;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        public static string GetRootNamespace(this Project project)
        {
            if (project == null)
                return null;

            string ns = project.Name ?? string.Empty;

            try
            {
                var prop = project.Properties.Item("RootNamespace");

                if (prop != null && prop.Value != null && !string.IsNullOrEmpty(prop.Value.ToString()))
                    ns = prop.Value.ToString();
            }
            catch { /* Project doesn't have a root namespace */ }

            return CleanNameSpace(ns, stripPeriods: false);
        }

        public static string CleanNameSpace(string ns, bool stripPeriods = true)
        {
            if (stripPeriods)
            {
                ns = ns.Replace(".", "");
            }

            ns = ns.Replace(" ", "")
                .Replace("-", "")
                .Replace("\\", ".");

            return ns;
        }
    }


    public static class ProjectTypes
    {
        public const string ASPNET_5 = "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}";
        public const string DOTNET_Core = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        public const string WEBSITE_PROJECT = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        public const string UNIVERSAL_APP = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        public const string NODE_JS = "{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";
        public const string SSDT = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
    }
}